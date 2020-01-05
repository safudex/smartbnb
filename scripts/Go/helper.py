import sys

# Base field Z_p
p = 2**255 - 19

def modp_inv(x):
    return pow(x, p-2, p)

# Curve constant
d = -121665 * modp_inv(121666) % p

# Square root of -1
modp_sqrt_m1 = pow(2, (p-1) // 4, p)

# Compute corresponding x-coordinate, with low bit corresponding to
# sign, or return None on failure
def recover_x(y, sign):
    if y >= p:
        return None
    x2 = (y*y-1) * modp_inv(d*y*y+1)
    if x2 == 0:
        if sign:
            return None
        else:
            return 0

    # Compute square root of x2
    x = pow(x2, (p+3) // 8, p)
    if (x*x - x2) % p != 0:
        x = x * modp_sqrt_m1 % p
    if (x*x - x2) % p != 0:
        return None

    if (x & 1) != sign:
        x = p - x
    return x

def point_decompress(s):
    if len(s) != 32:
        raise Exception("Invalid input length for decompression")
    y = int.from_bytes(s, "little")
    sign = y >> 255
    y &= (1 << 255) - 1

    x = recover_x(y, sign)
    if x is None:
        return None
    else:
        return (x, y, 1, x*y % p)

def leftpad(a, n):
    return "0"*(n-len(a))+a

def normalizedHex(n):
    hexed = hex(n)[2:]
    if(hexed[-1]=="L"):
        hexed=hexed[:-1]
    return hexed

def num2byteArray(num):
    hexed = normalizedHex(num)
    hexed = leftpad(hexed, 64)
    hexed = [hexed[i:i+2] for i in range(0, len(hexed), 2)]
    hexed.reverse()
    return "{0x"+", 0x".join(hexed)+"}"

def hexString2byteArray(total):
    return [int(str(total[i])+str(total[i+1]), 16) for i in range(0,len(total), 2)]

def get8bits(n):
	b = bin(n)[2:]
	b = "0"*(8-(len(b)))+b
	return b

def padMsg(msg):
	allbits = "".join([get8bits(msg[i]) for i in range(len(msg))])
	l = len(allbits)
	n = int(allbits, 2)
	n<<=1
	n|=1
	k=(896-l-1)%1024
	n<<=k
	n<<=128
	n|=l
	n=bin(n)[2:]
	n="0"*(1024-len(n)%1024)+n
	print([int(n[i:i+64], 2) for i in range(0, len(n), 64)])

def getXY(point):
	r = point_decompress(bytes.fromhex(point))
	print(r[0])
	print(r[1])
	print(num2byteArray(int(point, 16)))

op = sys.argv[1] 
if op == "1":
	getXY(sys.argv[2])
elif op == "2":
	padMsg(hexString2byteArray(sys.argv[2]))
