#define WIN32_LEAN_AND_MEAN
#include <windows.h>

const char* hello = "Hello Cpp";

extern "C" {
	__declspec(dllexport) char* FileCPP() {
		return const_cast<char *>(hello);
	}
}