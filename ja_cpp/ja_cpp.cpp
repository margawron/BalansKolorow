#define WIN32_LEAN_AND_MEAN
#include <windows.h>

const char* hello = "Hello Cpp";

extern "C" {
	// http://locklessinc.com/articles/sat_arithmetic/


	__declspec(dllexport) void AdditionBitmapColorBalancer(unsigned char* bitmap, int size,unsigned char* bgra) {
		// Dodaj z uwzglêdnieniem saturacji unikamy w ten sposób usterek w obrazie
		// Lambda pozwoli u³atwiæ kompilatorowi optymalizacjê
		auto saturateColors = [](unsigned char orignalComponent,unsigned char raiseComponentBy) {
			unsigned char result = orignalComponent + raiseComponentBy;
			return (result |= -(result < orignalComponent));
		};
		// Little endian sprawia ¿e bitmapa jest w formacie bgra natomiast argb nadal jest w 
		for (int i = 0; i < size; i += 4) {
			bitmap[i] = saturateColors(bitmap[i], bgra[0]);		// blue
			bitmap[i + 1] = saturateColors(bitmap[i + 1], bgra[1]);	// green
			bitmap[i + 2] = saturateColors(bitmap[i + 2], bgra[2]);	// red
			bitmap[i + 3] = saturateColors(bitmap[i + 3], bgra[3]);	// alpha
		}
	}

	__declspec(dllexport) void SubtractionBitmapColorBalancer(unsigned char* bitmap, int size, unsigned char* bgra) {
		auto saturateColors = [](unsigned char orginalComponent, unsigned char raiseComponentBy) {
			unsigned char result = orginalComponent - raiseComponentBy;
			return result &= -(result <= orginalComponent);
		};
		for (int i = 0; i < size; i += 4) {
			bitmap[i] = saturateColors(bitmap[i], bgra[0]);		// blue
			bitmap[i + 1] = saturateColors(bitmap[i + 1], bgra[1]);	// green
			bitmap[i + 2] = saturateColors(bitmap[i + 2], bgra[2]);	// red
			bitmap[i + 3] = saturateColors(bitmap[i + 3], bgra[3]);	// alpha
		}
	}

	__declspec(dllexport) void MultiplicationColorBalancer(unsigned char* bitmap, int size, float* bgra) {
		// Little endian sprawia ¿e bitmapa jest w formacie bgra
		auto saturateColors = [](unsigned char originalComponent, float raiseComponentBy) {
			unsigned short result = (float)originalComponent * raiseComponentBy;
			unsigned char highByte = result >> 8;
			unsigned char lowByte = (unsigned char) result;
			unsigned char returnVal = lowByte | -!!highByte;
			return returnVal;
		};
		for (int i = 0; i < size; i += 4) {
			bitmap[i] = saturateColors(bitmap[i], bgra[0]);
			bitmap[i + 1] = saturateColors(bitmap[i + 1], bgra[1]);
			bitmap[i + 2] = saturateColors(bitmap[i + 2], bgra[2]);
			bitmap[i + 3] = saturateColors(bitmap[i + 3], bgra[3]);
		}
	}
}