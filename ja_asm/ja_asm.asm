


.data
text DB "Hello ASM", 0FFH


.code 
FileASM proc
	mov rax, OFFSET text
	ret
FileASM endp

end
