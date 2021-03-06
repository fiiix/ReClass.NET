#include <iostream>
#include <string>
#include <sstream>
#include <fstream>
#include <experimental/filesystem>

#include "NativeCore.hpp"

namespace fs = std::experimental::filesystem;

// std::filesystem library doesn't work @Ubuntu 16.10, read_symlink() always fails.
#define USE_CUSTOM_READ_SYMLINK

#ifdef USE_CUSTOM_READ_SYMLINK
#include <unistd.h>

fs::path my_read_symlink(const fs::path& p, std::error_code& ec)
{
	fs::path symlink_path;

	std::string temp(64, '\0');
	for (;; temp.resize(temp.size() * 2))
	{
		ssize_t result;
		if ((result = ::readlink(p.c_str(), /*temp.data()*/ &temp[0], temp.size())) == -1)
		{
			ec.assign(errno, std::system_category());
			break;
		}
		else
		{
			if (result != (ssize_t)temp.size())
			{
				symlink_path = fs::path(std::string(temp.begin(), temp.begin() + result));

				ec.clear();

				break;
			}
		}
	}

	return symlink_path;
}

#endif

bool is_number(const std::string& s)
{
	auto it = s.begin();
	for (; it != s.end() && std::isdigit(*it); ++it);
	return !s.empty() && it == s.end();
}

template<typename T>
T parse_type(const std::string& s)
{
	std::stringstream ss(s);

	T val;
	ss >> val;
	return val;
}

enum class Platform
{
	Unknown,
	X86,
	X64
};

Platform GetProcessPlatform(const std::string& auxvPath)
{
	auto platform = Platform::Unknown;

	std::ifstream file(auxvPath);
	if (file)
	{
		char buffer[16];
		while (true)
		{
			file.read(buffer, 16);

			if (!file)
			{
				return Platform::X64;
			}

			if (buffer[4] != 0 || buffer[5] != 0 || buffer[6] != 0 || buffer[7] != 0)
			{
				return Platform::X86;
			}
		}
	}

	return platform;
}

extern "C" void EnumerateProcesses(EnumerateProcessCallback callbackProcess)
{
	if (callbackProcess == nullptr)
	{
		return;
	}

	fs::path procPath("/proc");
	if (fs::is_directory(procPath))
	{
		for (auto& d : fs::directory_iterator(procPath))
		{
			if (fs::is_directory(d))
			{
				auto processPath = d.path();

				auto name = processPath.filename().string();
				if (is_number(name))
				{
					auto exeSymLink = processPath / "exe";
					if (fs::is_symlink(fs::symlink_status(exeSymLink)))
					{
						std::error_code ec;
						auto linkPath = 
#ifdef USE_CUSTOM_READ_SYMLINK
							my_read_symlink
#else
							read_symlink
#endif
							(exeSymLink, ec).string();
						if (!ec)
						{
							auto auxvPath = processPath / "auxv";

							auto platform = GetProcessPlatform(auxvPath.string());
#ifdef NATIVE_CORE_64
							if (platform == Platform::X64)
#else
							if (platform == Platform::X86)
#endif
							{
								EnumerateProcessData data = {};
								data.Id = parse_type<size_t>(name);
								MultiByteToUnicode(linkPath.c_str(), data.ModulePath, PATH_MAXIMUM_LENGTH);

								callbackProcess(&data);
							}
						}
					}
				}
			}
		}
	}
}
