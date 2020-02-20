import sys
import hashlib

def sha512(s):
    return hashlib.sha512(s).digest()

def sha512_modq(s):
    h = sha512(s)
    return h, int.from_bytes(h, "little") % q

# Base field Z_p
p = 2**255 - 19

# Group order
q = 2**252 + 27742317777372353535851937790883648493

def modp_inv(x):
    return pow(x, p-2, p)

# Curve constant
d = -121665 * modp_inv(121666) % p

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



# Base point
g_y = 4 * modp_inv(5) % p
g_x = recover_x(g_y, 0)
G = (g_x, g_y, 1, g_x * g_y % p)

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
        n="0"*((1024-len(n))%1024)+n
        print(",".join([str(int(n[i:i+64], 2))for i in range(0, len(n), 64)]))

def sBhA(msgBytes, sig, public):
        msghash, hashmod = sha512_modq(msgBytes)
        msghash=msghash.hex()
        print(",".join([str(int(msghash[i:i+16], 16)) for i in range(0, len(msghash), 16)]))
        print(hashmod)
        s = int.from_bytes(sig[32:], "little")
        print(s)
        sB = point_mul(s, G)
        print(str(sB[0])+","+str(sB[1])+","+str(sB[2])+","+str(sB[3]))
        A = point_decompress(bytes.fromhex(public))
        hA = point_mul(hashmod, A)
        print(str(hA[0])+","+str(hA[1])+","+str(hA[2])+","+str(hA[3]))

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
    P = (P[0]%p, P[1]%p, P[2]%p, P[3]%p)
    Q = (Q[0]%p, Q[1]%p, Q[2]%p, Q[3]%p)
    A, B = (P[1]-P[0]) * (Q[1]-Q[0]) % p, (P[1]+P[0]) * (Q[1]+Q[0]) % p;
    C, D = 2 * P[3] * Q[3] * d % p, 2 * P[2] * Q[2] % p;
    E, F, G, H = B-A, D-C, D+C, B+A;
    return ((E*F)%p, (G*H)%p, (F*G)%p, (E*H)%p);

# Computes Q = s * Q
def point_mul(s, P):
        Q = (0, 1, 1, 0)  # Neutral element
        while s > 0:
                if s & 1:
                        Q = point_add(Q, P)
                P = point_add(P, P)
                s >>= 1
                Q = (Q[0]%p, Q[1]%p, Q[2]%p, Q[3]%p)
                P = (P[0]%p, P[1]%p, P[2]%p, P[3]%p)
        return Q

def point_mul_step(s, P, Q):
        Q = (Q[0]%p, Q[1]%p, Q[2]%p, Q[3]%p)
        P = (P[0]%p, P[1]%p, P[2]%p, P[3]%p)
        if s > 0:
            if s & 1:
                    Q = point_add(Q, P)
            P = point_add(P, P)
            s >>= 1
        return s, P, Q

def point_mul_by_it(s, P, Q, its):
        _s, _P, _Q = s, P, Q
        for i in range(its):
                _s, _P, _Q = point_mul_step(_s, _P, _Q)
        _Q = (_Q[0]%p, _Q[1]%p, _Q[2]%p, _Q[3]%p)
        _P = (_P[0]%p, _P[1]%p, _P[2]%p, _P[3]%p)
        return _s, _P, _Q

def getPointMul(s, P, Q, its):
        s, its = int(s), int(its)
        for i in range((256//its)):
                s, P, Q = point_mul_by_it(s, P, Q, its)
                Q = (Q[0]%p, Q[1]%p, Q[2]%p, Q[3]%p)
                P = (P[0]%p, P[1]%p, P[2]%p, P[3]%p)
                print(s)
                print(str(P[0])+","+str(P[1])+","+str(P[2])+","+str(P[3]))
                print(str(Q[0])+","+str(Q[1])+","+str(Q[2])+","+str(Q[3]))

op = sys.argv[1] 
if op == "1":
        getXY(sys.argv[2])
elif op == "2":
        padMsg(hexString2byteArray(sys.argv[2]))
elif op == "3":
        sBhA(bytes.fromhex(sys.argv[2]), bytes.fromhex(sys.argv[3]), sys.argv[4])
elif op == "4":
        isHA = sys.argv[2]
        if isHA == "true":
            G = point_decompress(bytes.fromhex(sys.argv[5]))
        getPointMul(sys.argv[3], G, (0, 1, 1, 0), sys.argv[4])
