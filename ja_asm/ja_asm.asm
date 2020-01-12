

.data
text DB "Hello ASM", 0FFH


.code 
AdditionBitmapColorBalancer PROC bitmapPointer:QWORD, bitmapSize:QWORD, argbPointer:QWORD
	; kolejno�� rejestr�w przechowuj�cych parametry 
	; RCX,RDX,R8,R9,XMM0-XMM3,YMM0-YMM3,ZMM0-ZMM3
	; rejestry do dowolnego zapisu
	; RAX,RCX,RDX,R8-R11,ST(0)-ST(7),XMM0-XMM5,ALL YMM/ZMM z wyj�tkiem dolnych 128 bit�w XMM6-XMM15
	; https://www.agner.org/optimize/calling_conventions.pdf - 64bit WINDOWS
	; rcx - bitmapPointer, rdx - bitmapSize; r8 - argbPointer
	mov eax,DWORD PTR[r8]	; pobierz tablic� warto�ci argb z podanego adresu do akumulatora
	pinsrd xmm1,eax,0		; za�aduj i wype�nij tablic� argb do rejestru simd
	pinsrd xmm1,eax,1		; j.w.
	pinsrd xmm1,eax,2		; j.w
	pinsrd xmm1,eax,3		; j.w
add_batch_work:
    cmp rdx,16					; sprawd� czy jeste�my w stanie zape�ni� ca�y rejestr SIMD warto�ciami pixeli
	jb add_single_work			; je�eli jest mniej pracy do zrobienia wykonaj� j� na pojedynczym pixelu
	; aby wzi��� pod uwag� to i� b�dziemy przetwarza� tablic� pixeli od ko�ca 
	; musimy odj�� ca�y rozmiar rejestru od podstawy wska�nika by pobiera� poprawne dane
	movups xmm0, [rcx+rdx-16]	; za�aduj pixele z pod adresu wys�anego przez kod c#
	paddusb xmm0,xmm1			; dodanie z saturacj� (je�eli dochodzi do przepe�nienia wpisz maksymaln� mieszcz�c� si� warto��)
	movups [rcx+rdx-16], xmm0	; zapisz wynik
	sub rdx, 16					; zmniejsz liczb� pixeli do przetworzenia o 4 (4 pixele * 4 komponenty pixela)
	jmp add_batch_work			; powt�rz na ca�ej serii pixeli
add_single_work:
	cmp rdx,0						; sprawdz czy to by� ostatni pixel
	je add_no_work					; je�eli to by� ostatni pixel to wyjd�
	movd xmm0, DWORD PTR[rcx+rdx-4]	; pobierz pixel
	paddusb	xmm0,xmm1				; dodaj z saturacj�
	movd DWORD PTR [rcx+rdx-4],xmm0	; zapisz pixel
	sub rdx, 4						; zmniejsz liczb� pixeli do przetworzenia o 1
	jmp add_single_work				; powt�rz na pojedynczym pixelu
add_no_work:
	ret						; wyjd�
AdditionBitmapColorBalancer endp
SubtractionBitmapColorBalancer PROC bitmapPointer:QWORD, bitmapSize:QWORD, argbPointer:QWORD
	; kolejno�� rejestr�w przechowuj�cych parametry 
	; RCX,RDX,R8,R9,XMM0-XMM3,YMM0-YMM3,ZMM0-ZMM3
	; rejestry do dowolnego zapisu
	; RAX,RCX,RDX,R8-R11,ST(0)-ST(7),XMM0-XMM5,ALL YMM/ZMM z wyj�tkiem dolnych 128 bit�w XMM6-XMM15
	; https://www.agner.org/optimize/calling_conventions.pdf - 64bit WINDOWS
	; rcx - bitmapPointer, rdx - bitmapSize; r8 - argbPointer
	mov eax,DWORD PTR[r8]; pobierz tablic� warto�ci argb z podanego adresu do akumulatora
	pinsrd xmm1,eax,0	; za�aduj i wype�nij tablic� argb do rejestru simd
	pinsrd xmm1,eax,1	; j.w.
	pinsrd xmm1,eax,2	; j.w
	pinsrd xmm1,eax,3	; j.w
sub_batch_work:
    cmp rdx,16					; sprawd� czy jeste�my w stanie zape�ni� ca�y rejestr SIMD warto�ciami pixeli
	jb sub_single_work			; je�eli jest mniej pracy do zrobienia wykonaj� j� na pojedynczym pixelu
	; aby wzi��� pod uwag� to i� b�dziemy przetwarza� tablic� pixeli od ko�ca 
	; musimy odj�� ca�y rozmiar rejestru od podstawy wska�nika by pobiera� poprawne dane
	movups xmm0, [rcx+rdx-16]	; za�aduj pixele z pod adresu wys�anego przez kod c#
	psubusb xmm0,xmm1			; odejmowanie z saturacj� (je�eli dochodzi do przepe�nienia wpisz maksymaln� mieszcz�c� si� warto��)
	movups [rcx+rdx-16], xmm0	; zapisz wynik
	sub rdx, 16					; zmniejsz liczb� pixeli do przetworzenia o 4 (4 pixele * 4 komponenty pixela)
	jmp sub_batch_work			; powt�rz na ca�ej serii pixeli
sub_single_work:
	cmp rdx,0					; sprawdz czy to by� ostatni pixel
	je sub_no_work					; je�eli to by� ostatni pixel to wyjd�
	movd xmm0, DWORD PTR[rcx+rdx-4]	; pobierz pixel
	psubusb	xmm0,xmm1				; odejmij z saturacj�
	movd DWORD PTR [rcx+rdx-4],xmm0	; zapisz pixel
	sub rdx, 4						; zmniejsz liczb� pixeli do przetworzenia o 1
	jmp sub_single_work			; powt�rz na pojedynczym pixelu
sub_no_work:
	ret						; wyjd�
SubtractionBitmapColorBalancer endp
MultiplicationColorBalancer PROC bitmapPointer:QWORD, bitmapSize:QWORD, argbPointer:QWORD
	; kolejno�� rejestr�w przechowuj�cych parametry 
	; RCX,RDX,R8,R9,XMM0-XMM3,YMM0-YMM3,ZMM0-ZMM3
	; rejestry do dowolnego zapisu
	; RAX,RCX,RDX,R8-R11,ST(0)-ST(7),XMM0-XMM5,ALL YMM/ZMM z wyj�tkiem dolnych 128 bit�w XMM6-XMM15
	; https://www.agner.org/optimize/calling_conventions.pdf - 64bit WINDOWS
	; rcx - bitmapPointer, rdx - bitmapSize; r8 - argbPointer
	; za�adowa� do rejestru XMM0 warto�ci bitmapy
	movups xmm1, XMMWORD PTR[r8]; pobierz tablic� konwersji (float�w) do rejestru
mult_work:
    cmp rdx, 0			; sprawd� czy jeste�my w stanie zape�ni� ca�y rejestr SIMD warto�ciami jednego pixela
	jz mult_no_work
	pmovzxbd xmm0, DWORD PTR [rcx+rdx-4] ; za�aduj 4 bajty oraz rozszerz je do integer�w (8-bit na 32-bity)
	cvtdq2ps xmm0,xmm0	; przekonwertuj inty do float�w
	mulps xmm0,xmm1		; pomn� przekonwertowane komponenty pixela przez tablic� konwersji
	cvtps2dq xmm0,xmm0	; przekonwertuj pomno�ony wynik na integery (float -> int)
	pabsd xmm0,xmm0		; wyci�gnij warto�� bezwzgl�dn�
	packusdw xmm0,xmm0  ; przekonwertuj na 16-bit�w (32-bit -> 16-bit)
	packuswb xmm0,xmm0	; przekonwertuj na 8-bit�w (16-bit -> 8-bit)
	movd DWORD PTR[rcx+rdx-4],xmm0 ; zapisz wynik
	sub rdx, 4			; pomniejsz liczb� pixeli do przetworzenia o 1
	jmp mult_work		; powt�rz pojedynczy pixel
mult_no_work:
	ret						; wyjd�
MultiplicationColorBalancer endp
END

