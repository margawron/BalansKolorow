

.data
text DB "Hello ASM", 0FFH


.code 
AdditionBitmapColorBalancer PROC bitmapPointer:QWORD, bitmapSize:QWORD, argbPointer:QWORD
	; kolejnoœæ rejestrów przechowuj¹cych parametry 
	; RCX,RDX,R8,R9,XMM0-XMM3,YMM0-YMM3,ZMM0-ZMM3
	; rejestry do dowolnego zapisu
	; RAX,RCX,RDX,R8-R11,ST(0)-ST(7),XMM0-XMM5,ALL YMM/ZMM z wyj¹tkiem dolnych 128 bitów XMM6-XMM15
	; https://www.agner.org/optimize/calling_conventions.pdf - 64bit WINDOWS
	; rcx - bitmapPointer, rdx - bitmapSize; r8 - argbPointer
	mov eax,DWORD PTR[r8]	; pobierz tablicê wartoœci argb z podanego adresu do akumulatora
	pinsrd xmm1,eax,0		; za³aduj i wype³nij tablicê argb do rejestru simd
	pinsrd xmm1,eax,1		; j.w.
	pinsrd xmm1,eax,2		; j.w
	pinsrd xmm1,eax,3		; j.w
add_batch_work:
    cmp rdx,16					; sprawdŸ czy jesteœmy w stanie zape³niæ ca³y rejestr SIMD wartoœciami pixeli
	jb add_single_work			; je¿eli jest mniej pracy do zrobienia wykonaj¹ j¹ na pojedynczym pixelu
	; aby wzi¹œæ pod uwagê to i¿ bêdziemy przetwarzaæ tablicê pixeli od koñca 
	; musimy odj¹æ ca³y rozmiar rejestru od podstawy wskaŸnika by pobieraæ poprawne dane
	movups xmm0, [rcx+rdx-16]	; za³aduj pixele z pod adresu wys³anego przez kod c#
	paddusb xmm0,xmm1			; dodanie z saturacj¹ (je¿eli dochodzi do przepe³nienia wpisz maksymaln¹ mieszcz¹c¹ siê wartoœæ)
	movups [rcx+rdx-16], xmm0	; zapisz wynik
	sub rdx, 16					; zmniejsz liczbê pixeli do przetworzenia o 4 (4 pixele * 4 komponenty pixela)
	jmp add_batch_work			; powtórz na ca³ej serii pixeli
add_single_work:
	cmp rdx,0						; sprawdz czy to by³ ostatni pixel
	je add_no_work					; je¿eli to by³ ostatni pixel to wyjdŸ
	movd xmm0, DWORD PTR[rcx+rdx-4]	; pobierz pixel
	paddusb	xmm0,xmm1				; dodaj z saturacj¹
	movd DWORD PTR [rcx+rdx-4],xmm0	; zapisz pixel
	sub rdx, 4						; zmniejsz liczbê pixeli do przetworzenia o 1
	jmp add_single_work				; powtórz na pojedynczym pixelu
add_no_work:
	ret						; wyjdŸ
AdditionBitmapColorBalancer endp
SubtractionBitmapColorBalancer PROC bitmapPointer:QWORD, bitmapSize:QWORD, argbPointer:QWORD
	; kolejnoœæ rejestrów przechowuj¹cych parametry 
	; RCX,RDX,R8,R9,XMM0-XMM3,YMM0-YMM3,ZMM0-ZMM3
	; rejestry do dowolnego zapisu
	; RAX,RCX,RDX,R8-R11,ST(0)-ST(7),XMM0-XMM5,ALL YMM/ZMM z wyj¹tkiem dolnych 128 bitów XMM6-XMM15
	; https://www.agner.org/optimize/calling_conventions.pdf - 64bit WINDOWS
	; rcx - bitmapPointer, rdx - bitmapSize; r8 - argbPointer
	mov eax,DWORD PTR[r8]; pobierz tablicê wartoœci argb z podanego adresu do akumulatora
	pinsrd xmm1,eax,0	; za³aduj i wype³nij tablicê argb do rejestru simd
	pinsrd xmm1,eax,1	; j.w.
	pinsrd xmm1,eax,2	; j.w
	pinsrd xmm1,eax,3	; j.w
sub_batch_work:
    cmp rdx,16					; sprawdŸ czy jesteœmy w stanie zape³niæ ca³y rejestr SIMD wartoœciami pixeli
	jb sub_single_work			; je¿eli jest mniej pracy do zrobienia wykonaj¹ j¹ na pojedynczym pixelu
	; aby wzi¹œæ pod uwagê to i¿ bêdziemy przetwarzaæ tablicê pixeli od koñca 
	; musimy odj¹æ ca³y rozmiar rejestru od podstawy wskaŸnika by pobieraæ poprawne dane
	movups xmm0, [rcx+rdx-16]	; za³aduj pixele z pod adresu wys³anego przez kod c#
	psubusb xmm0,xmm1			; odejmowanie z saturacj¹ (je¿eli dochodzi do przepe³nienia wpisz maksymaln¹ mieszcz¹c¹ siê wartoœæ)
	movups [rcx+rdx-16], xmm0	; zapisz wynik
	sub rdx, 16					; zmniejsz liczbê pixeli do przetworzenia o 4 (4 pixele * 4 komponenty pixela)
	jmp sub_batch_work			; powtórz na ca³ej serii pixeli
sub_single_work:
	cmp rdx,0					; sprawdz czy to by³ ostatni pixel
	je sub_no_work					; je¿eli to by³ ostatni pixel to wyjdŸ
	movd xmm0, DWORD PTR[rcx+rdx-4]	; pobierz pixel
	psubusb	xmm0,xmm1				; odejmij z saturacj¹
	movd DWORD PTR [rcx+rdx-4],xmm0	; zapisz pixel
	sub rdx, 4						; zmniejsz liczbê pixeli do przetworzenia o 1
	jmp sub_single_work			; powtórz na pojedynczym pixelu
sub_no_work:
	ret						; wyjdŸ
SubtractionBitmapColorBalancer endp
MultiplicationColorBalancer PROC bitmapPointer:QWORD, bitmapSize:QWORD, argbPointer:QWORD
	; kolejnoœæ rejestrów przechowuj¹cych parametry 
	; RCX,RDX,R8,R9,XMM0-XMM3,YMM0-YMM3,ZMM0-ZMM3
	; rejestry do dowolnego zapisu
	; RAX,RCX,RDX,R8-R11,ST(0)-ST(7),XMM0-XMM5,ALL YMM/ZMM z wyj¹tkiem dolnych 128 bitów XMM6-XMM15
	; https://www.agner.org/optimize/calling_conventions.pdf - 64bit WINDOWS
	; rcx - bitmapPointer, rdx - bitmapSize; r8 - argbPointer
	; za³adowaæ do rejestru XMM0 wartoœci bitmapy
	movups xmm1, XMMWORD PTR[r8]; pobierz tablicê konwersji (floatów) do rejestru
mult_work:
    cmp rdx, 0			; sprawdŸ czy jesteœmy w stanie zape³niæ ca³y rejestr SIMD wartoœciami jednego pixela
	jz mult_no_work
	pmovzxbd xmm0, DWORD PTR [rcx+rdx-4] ; za³aduj 4 bajty oraz rozszerz je do integerów (8-bit na 32-bity)
	cvtdq2ps xmm0,xmm0	; przekonwertuj inty do floatów
	mulps xmm0,xmm1		; pomnó¿ przekonwertowane komponenty pixela przez tablicê konwersji
	cvtps2dq xmm0,xmm0	; przekonwertuj pomno¿ony wynik na integery (float -> int)
	pabsd xmm0,xmm0		; wyci¹gnij wartoœæ bezwzglêdn¹
	packusdw xmm0,xmm0  ; przekonwertuj na 16-bitów (32-bit -> 16-bit)
	packuswb xmm0,xmm0	; przekonwertuj na 8-bitów (16-bit -> 8-bit)
	movd DWORD PTR[rcx+rdx-4],xmm0 ; zapisz wynik
	sub rdx, 4			; pomniejsz liczbê pixeli do przetworzenia o 1
	jmp mult_work		; powtórz pojedynczy pixel
mult_no_work:
	ret						; wyjdŸ
MultiplicationColorBalancer endp
END

