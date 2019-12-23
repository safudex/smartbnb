def num2byteArray(num):
    hexed = hex(num)[2:]
    if(hexed[-1]=="L"):
        hexed=hexed[:-1]
    hexed = [hexed[i:i+2] for i in range(0, len(hexed), 2)]
    hexed.reverse()
    return "{0x"+", 0x".join(hexed)+"}"

p = 2**255 - 19
print("p = ", num2byteArray(p))
