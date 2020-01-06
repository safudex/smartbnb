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

# Base field Z_p
p = 2**255 - 19

def modp_inv(x):
    return pow(x, p-2, p)

# Curve constant
d = -121665 * modp_inv(121666) % p

def point_add(P, Q):
    A, B = (P[1]-P[0]) * (Q[1]-Q[0]) % p, (P[1]+P[0]) * (Q[1]+Q[0]) % p;
    C, D = 2 * P[3] * Q[3] * d % p, 2 * P[2] * Q[2] % p;
    E, F, G, H = B-A, D-C, D+C, B+A;
    return (E*F, G*H, F*G, E*H);

# Computes Q = s * Q
def point_mul(s, P):
	Q = (0, 1, 1, 0)  # Neutral element
	while s > 0:
		if s & 1:
			Q = point_add(Q, P)
		P = point_add(P, P)
		s >>= 1
	return Q

def point_mul_step(s, P, Q):
	if s & 1:
		Q = point_add(Q, P)
	P = point_add(P, P)
	s >>= 1
	return s, P, Q

def point_mul_by_it(s, P, Q, its):
	for i in range(its):
		s, P, Q = point_mul_step(s, P, Q)
	return s, P, Q


op = sys.argv[1] 
if op == "1":
	getXY(sys.argv[2])
elif op == "2":
	padMsg(hexString2byteArray(sys.argv[2]))
'''
# Base point
g_y = 4 * modp_inv(5) % p
g_x = recover_x(g_y, 0)
G = (g_x, g_y, 1, g_x * g_y % p)
signature = "11fd72472aba93cfc207acd5b948badf2c35da2d5b2b2a9623265fd57670f51062e0201f5314f51fd4848b0bc958d15f1fa6f7ab90af7179be545086071ba501"
s = int.from_bytes(bytes.fromhex(signature[32:]), "little")
print(point_mul_by_it(s, G, (0, 1, 1, 0), 10)[0])
'''
