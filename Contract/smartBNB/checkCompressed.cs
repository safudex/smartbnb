using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.Numerics;

namespace Neo.SmartContract
{
    public class HelloWorld : Framework.SmartContract
    {
        public static bool Main()
        {
            // writes value "World" on storage key "Hello"
            // implicitly calls Storage.Put(Storage.CurrentContext, "Hello", "World")
            byte[] byteP = {0xed, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x7f};
			BigInteger p = byteP.AsBigInteger();
			
			byte[] byteX = {0xce, 0x45, 0x76, 0x77, 0xbd, 0x86, 0x27, 0xb1, 0x24, 0x7c, 0x18, 0x53, 0x72, 0xd4, 0x13, 0xc5, 0x20, 0xf6, 0xd0, 0x60, 0x8d, 0xe0, 0x97, 0x22, 0x29, 0x34, 0x9d, 0x2b, 0x9a, 0xe0, 0xd0, 0x55};
			BigInteger x = byteX.AsBigInteger();
			byte[] byteY = {0xd7, 0x5a, 0x98, 0x01, 0x82, 0xb1, 0x0a, 0xb7, 0xd5, 0x4b, 0xfe, 0xd3, 0xc9, 0x64, 0x07, 0x3a, 0x0e, 0xe1, 0x72, 0xf3, 0xda, 0xa6, 0x23, 0x25, 0xaf, 0x02, 0x1a, 0x68, 0xf7, 0x07, 0x51, 0x1a};
			BigInteger y = byteY.AsBigInteger();
			byte[] compressed = {0xd7, 0x5a, 0x98, 0x01, 0x82, 0xb1, 0x0a, 0xb7, 0xd5, 0x4b, 0xfe, 0xd3, 0xc9, 0x64, 0x07, 0x3a, 0x0e, 0xe1, 0x72, 0xf3, 0xda, 0xa6, 0x23, 0x25, 0xaf, 0x02, 0x1a, 0x68, 0xf7, 0x07, 0x51, 0x1a};
			    
			bool flag = checkCompressed(x, y, compressed, p);
			if(flag == true){
			    Storage.Put("Hello", "true");
			} else {
			    Storage.Put("Hello", "false");
			}
			
			return flag;
        }
        
        private static bool checkCompressed (BigInteger x, BigInteger y, byte[] compressed, BigInteger p){
            if(x<0 || x>=p){
                Storage.Put("cuck", "x");
                return false;
            }
            if(y<0 || y>=p){
                Storage.Put("cuck", "y");
                return false;
            }
            //Check that point belongs in the curve
            BigInteger x2 = mulmod(x, x, p);
            BigInteger y2 = mulmod(y, y, p);
            //((-x**2+y**2-1)*121666)%p==(-121665*x**2*y**2)%p
            if(mulmod(((modrest(y2, x2, p)-1)%p), 121666, p) != mulmod(p-121665, mulmod(x2, y2, p), p)){
                Storage.Put("cuck", "eq");
                return false; //Point doesn't belong in the curve
            }
            //int.to_bytes(y | ((x & 1) << 255), 32, "little")
            int sign = compressed[31] & 0x80;
            if((sign>>7) != (x%2)){
                Storage.Put("cuck", "sign");
                Storage.Put("compressed", compressed[31]);
                Storage.Put("sign", sign);
                return false; //Compressed is wrong
            }
            
            int index31 = 31;
            byte withSign = compressed[31];
            byte noSign = (byte)(compressed[31] & 0x7F);
            compressed[index31] = noSign;
            if(compressed.AsBigInteger() != y){
                Storage.Put("cuck", "num");
                return false;
            }
            
            compressed[index31] = withSign;
            
            return true;
        }
        
        private static BigInteger mulmod(BigInteger a, BigInteger b, BigInteger mod)  
        {  
            BigInteger res = 0; // Initialize result  

            while (b > 0)  
            {  
                // If b is odd, add 'a' to result  
                if (b % 2 == 1)  
                {  
                    //res = (res + a) % mod;
                    res = modsum(res, a, mod);
                }  

                // Multiply 'a' with 2  
                //a = (a * 2) % mod;  
                a = modsum(a, a, mod);

                // Divide b by 2  
                b /= 2;  
            }  

            // Return result  
            return res;  
        }
	
        private static BigInteger modsum(BigInteger a, BigInteger b, BigInteger p)
        {
            BigInteger k = (a-p)+b;
            if (k<0){
                k+=p;
            }
            return k;
        }
        
        private static BigInteger modrest(BigInteger a, BigInteger b, BigInteger p)
        {
            BigInteger r = a-b;
            return r < 0 ? r + p : r;
        }
    }
}

