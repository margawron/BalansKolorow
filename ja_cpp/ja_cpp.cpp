#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <thread>
#include <chrono>

const char* hello = "Hello Cpp";

using okres = std::chrono::duration<int, std::pico>;
	
extern "C" {


	__declspec(dllexport) void AdditionBitmapColorBalancer(unsigned char* bitmap, int size, unsigned char* bgra, unsigned int sleepFor) {
		// Dodaj z uwzglêdnieniem saturacji unikamy w ten sposób usterek w obrazie
		// Lambda pozwoli u³atwiæ kompilatorowi optymalizacjê i poprawia czytelnoœæ
		auto saturateColors = [](unsigned char orignalComponent, unsigned char raiseComponentBy) {
			unsigned char result = orignalComponent + raiseComponentBy;
			return (result |= -(result < orignalComponent));
		};
		// Little endian sprawia ¿e bitmapa jest w formacie bgra zamiast argb
		if (sleepFor == 0) {
			for (int i = 0; i < size; i += 4) {
				bitmap[i] = saturateColors(bitmap[i], bgra[0]);		// blue
				bitmap[i + 1] = saturateColors(bitmap[i + 1], bgra[1]);	// green
				bitmap[i + 2] = saturateColors(bitmap[i + 2], bgra[2]);	// red
				bitmap[i + 3] = saturateColors(bitmap[i + 3], bgra[3]);	// alpha
			}
		}
		else {
			for (int i = 0; i < size; i += 4) {
				bitmap[i] = saturateColors(bitmap[i], bgra[0]);		// blue
				bitmap[i + 1] = saturateColors(bitmap[i + 1], bgra[1]);	// green
				bitmap[i + 2] = saturateColors(bitmap[i + 2], bgra[2]);	// red
				bitmap[i + 3] = saturateColors(bitmap[i + 3], bgra[3]);	// alpha
				std::this_thread::sleep_for(okres(sleepFor));
			}
		}

	}

	__declspec(dllexport) void SubtractionBitmapColorBalancer(unsigned char* bitmap, int size, unsigned char* bgra, unsigned int sleepFor) {
		auto saturateColors = [](unsigned char orginalComponent, unsigned char raiseComponentBy) {
			unsigned char result = orginalComponent - raiseComponentBy;
			return result &= -(result <= orginalComponent);
		};
		if (sleepFor == 0) {
			for (int i = 0; i < size; i += 4) {
				bitmap[i] = saturateColors(bitmap[i], bgra[0]);		// blue
				bitmap[i + 1] = saturateColors(bitmap[i + 1], bgra[1]);	// green
				bitmap[i + 2] = saturateColors(bitmap[i + 2], bgra[2]);	// red
				bitmap[i + 3] = saturateColors(bitmap[i + 3], bgra[3]);	// alpha
			}
		}
		else {
			for (int i = 0; i < size; i += 4) {
				bitmap[i] = saturateColors(bitmap[i], bgra[0]);		// blue
				bitmap[i + 1] = saturateColors(bitmap[i + 1], bgra[1]);	// green
				bitmap[i + 2] = saturateColors(bitmap[i + 2], bgra[2]);	// red
				bitmap[i + 3] = saturateColors(bitmap[i + 3], bgra[3]);	// alpha
				std::this_thread::sleep_for(okres(sleepFor));
			}
		}
	}

	__declspec(dllexport) void MultiplicationColorBalancer(unsigned char* bitmap, int size, float* bgra, int sleepFor) {
		// Little endian sprawia ¿e bitmapa jest w formacie bgra
		auto saturateColors = [](unsigned char originalComponent, float raiseComponentBy) {
			unsigned short result = (float)originalComponent * raiseComponentBy;
			unsigned char highByte = result >> 8;
			unsigned char lowByte = (unsigned char)result;
			unsigned char returnVal = lowByte | -!!highByte;
			return returnVal;
		};
		if (sleepFor == 0) {
			for (int i = 0; i < size; i += 4) {
				bitmap[i] = saturateColors(bitmap[i], bgra[0]);		// blue
				bitmap[i + 1] = saturateColors(bitmap[i + 1], bgra[1]);	// green
				bitmap[i + 2] = saturateColors(bitmap[i + 2], bgra[2]);	// red
				bitmap[i + 3] = saturateColors(bitmap[i + 3], bgra[3]);	// alpha
			}
		}
		else {
			for (int i = 0; i < size; i += 4) {
				bitmap[i] = saturateColors(bitmap[i], bgra[0]);		// blue
				bitmap[i + 1] = saturateColors(bitmap[i + 1], bgra[1]);	// green
				bitmap[i + 2] = saturateColors(bitmap[i + 2], bgra[2]);	// red
				bitmap[i + 3] = saturateColors(bitmap[i + 3], bgra[3]);	// alpha
				std::this_thread::sleep_for(okres(sleepFor));
			}
		}
	}
}