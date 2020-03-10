from decimal import Decimal, getcontext
from math import log2, ceil
from utils import num2byteArray
getcontext().prec=10000 # https://www.youtube.com/watch?v=Q1zBtJhgwBI

BNB_SUPPLY = 187536713
MAXBITS_SBNB = ceil(log2(BNB_SUPPLY*10**8))
PRECISION_BITS = 255-MAXBITS_SBNB
DENOMINATOR = 2**PRECISION_BITS

NUM_ITERATIONS = 20 # Highest iteration accounts for ~1/2 year
ITERATION_BASE = 2 # Growth per iteration, a lot of things assume this is 2 so changing this is not enough

ANNUAL_INTEREST = Decimal(5) / Decimal(100) # 5%
ANNUAL_FACTOR = Decimal(1) - ANNUAL_INTEREST
SECONDS_PER_YEAR = Decimal(365*24*3600)

# ANNUAL_FACTOR = BLOCK_FACTOR**SECONDS_PER_YEAR -> BLOCK_FACTOR=ANNUAL_FACTOR**(1/SECONDS_PER_YEAR)
BLOCK_FACTOR = ANNUAL_FACTOR**(1/SECONDS_PER_YEAR)

def BigInteger(name, value):
    return f"\n\
byte[] {name}Bytes = {num2byteArray(value)};\n\
BigInteger {name} = {name}Bytes.AsBigInteger();\n"

i = NUM_ITERATIONS
code = BigInteger("rateDenominator", DENOMINATOR)
code += BigInteger("magnitude", 2**NUM_ITERATIONS)
code += "byte[][] rateNumeratorBytes = new byte[][] {\n"
while i>=0:
    code += "new byte[]"+num2byteArray(round((BLOCK_FACTOR**(2**i))*DENOMINATOR))+",\n"
    i -= 1

print(code)
