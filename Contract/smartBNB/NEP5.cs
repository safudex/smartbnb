using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;
using Helper = Neo.SmartContract.Framework.Helper;

namespace NEP5
{
    public class NEP5 : SmartContract
    {
		[Serializable]
        struct Balance
        {
            public BigInteger amount;
            public BigInteger lastTimeTransfered;
        }

        [DisplayName("transfer")]
        public static event Action<byte[], byte[], BigInteger> Transferred;

        private static readonly byte[] Owner = "Ad1HKAATNmFT5buNgSxspbW68f4XVSssSw".ToScriptHash(); //Owner Address

        public static object Main(string method, object[] args)
        {
            if (Runtime.Trigger == TriggerType.Application)
            {
                var callscript = ExecutionEngine.CallingScriptHash;

                if (method == "balanceOf") return BalanceOf((byte[])args[0]);

                if (method == "decimals") return Decimals();

                if (method == "name") return Name();

                if (method == "symbol") return Symbol();

                if (method == "supportedStandards") return SupportedStandards();

                if (method == "totalSupply") return TotalSupply();

                if (method == "transfer") return Transfer((byte[])args[0], (byte[])args[1], (BigInteger)args[2], callscript);
            }
            return false;
        }

		private static Balance deserializeBalance(byte[] balance){
            if (balance.Length != 0){
				return new Balance() { amount = 0, lastTimeTransfered = 0 };
			} else {
				return (Balance)Helper.Deserialize(balance);
			}
		}

		private static BigInteger max(BigInteger a, BigInteger b){
			if(a >= b)
				return a;
			else
				return b;
		}

		private static BigInteger updateAmount(BigInteger amount, BigInteger lastTransfer, BigInteger currentTime){
			BigInteger deltaTime = currentTime - lastTransfer;

			// Code generated with NEP5interest.py, it essentially calculates the interest as in percentage^deltaTime in logarithmic time by using pre-computed constants
			byte[] rateDenominatorBytes = {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
			BigInteger rateDenominator = rateDenominatorBytes.AsBigInteger();
			while((deltaTime >> 20) > 0){
				byte[] rateNumeratorBytes = {0x1a, 0x62, 0x79, 0xa7, 0x12, 0xbb, 0x15, 0x5d, 0x6a, 0x9d, 0x79, 0xe0, 0x53, 0x41, 0xe9, 0x32, 0xe0, 0x79, 0x08, 0x90, 0x41, 0x4d, 0xae, 0x88, 0xf9, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
				BigInteger rateNumerator = rateNumeratorBytes.AsBigInteger();
				amount = (amount * rateNumerator) / rateDenominator;
				deltaTime -= (1 << 20);
			}
			if((deltaTime >> 19) > 0){
				byte[] rateNumeratorBytes = {0x66, 0x73, 0xa1, 0xc1, 0xf5, 0x24, 0xbe, 0x16, 0x2b, 0xe8, 0x57, 0xf7, 0xa9, 0x74, 0x1c, 0x3e, 0xd7, 0xad, 0x75, 0xea, 0x57, 0x0d, 0x0c, 0xbf, 0xfc, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
				BigInteger rateNumerator = rateNumeratorBytes.AsBigInteger();
				amount = (amount * rateNumerator) / rateDenominator;
				deltaTime -= (1 << 19);
			}
			if((deltaTime >> 18) > 0){
				byte[] rateNumeratorBytes = {0xb2, 0xc2, 0x96, 0xa7, 0x8e, 0x0e, 0x49, 0x6d, 0x01, 0xfd, 0x69, 0x56, 0x06, 0xc2, 0xd6, 0x4f, 0x75, 0xe4, 0x8a, 0x34, 0xcb, 0x14, 0x31, 0x5e, 0xfe, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
				BigInteger rateNumerator = rateNumeratorBytes.AsBigInteger();
				amount = (amount * rateNumerator) / rateDenominator;
				deltaTime -= (1 << 18);
			}
			if((deltaTime >> 17) > 0){
				byte[] rateNumeratorBytes = {0xfc, 0x9c, 0x70, 0x7d, 0x58, 0x12, 0x36, 0xb1, 0xa6, 0x50, 0xea, 0x94, 0x73, 0x6b, 0x2f, 0x7b, 0x31, 0x9e, 0x40, 0xf3, 0x17, 0x08, 0xc3, 0x2e, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
				BigInteger rateNumerator = rateNumeratorBytes.AsBigInteger();
				amount = (amount * rateNumerator) / rateDenominator;
				deltaTime -= (1 << 17);
			}
			if((deltaTime >> 16) > 0){
				byte[] rateNumeratorBytes = {0x30, 0xe6, 0xf6, 0x9c, 0x15, 0xe3, 0xaa, 0x5e, 0x5a, 0xe9, 0x7f, 0x37, 0x2c, 0xba, 0x67, 0x75, 0x3d, 0x9e, 0x00, 0x9f, 0xb7, 0x1a, 0x4c, 0x97, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
				BigInteger rateNumerator = rateNumeratorBytes.AsBigInteger();
				amount = (amount * rateNumerator) / rateDenominator;
				deltaTime -= (1 << 16);
			}
			if((deltaTime >> 15) > 0){
				byte[] rateNumeratorBytes = {0x2e, 0xae, 0x8c, 0xff, 0x4d, 0xb8, 0xfe, 0x13, 0xa1, 0xc8, 0xe9, 0xc6, 0x96, 0x50, 0x4a, 0xfb, 0x23, 0xd9, 0x5e, 0x36, 0xee, 0xb1, 0xa0, 0xcb, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
				BigInteger rateNumerator = rateNumeratorBytes.AsBigInteger();
				amount = (amount * rateNumerator) / rateDenominator;
				deltaTime -= (1 << 15);
			}
			if((deltaTime >> 14) > 0){
				byte[] rateNumeratorBytes = {0x54, 0x93, 0x74, 0x68, 0xef, 0x9b, 0xe5, 0x96, 0x8f, 0x8f, 0x02, 0xfc, 0x86, 0x9e, 0x71, 0x1d, 0x10, 0x15, 0x50, 0x9e, 0xf8, 0x01, 0xcf, 0xe5, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
				BigInteger rateNumerator = rateNumeratorBytes.AsBigInteger();
				amount = (amount * rateNumerator) / rateDenominator;
				deltaTime -= (1 << 14);
			}
			if((deltaTime >> 13) > 0){
				byte[] rateNumeratorBytes = {0x3d, 0x78, 0x00, 0x6b, 0xc7, 0xd6, 0xa6, 0xa9, 0xbe, 0xb3, 0x85, 0x70, 0x0d, 0x08, 0x13, 0x41, 0x6d, 0xc9, 0xb8, 0x4c, 0x38, 0x2b, 0xe7, 0xf2, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
				BigInteger rateNumerator = rateNumeratorBytes.AsBigInteger();
				amount = (amount * rateNumerator) / rateDenominator;
				deltaTime -= (1 << 13);
			}
			if((deltaTime >> 12) > 0){
				byte[] rateNumeratorBytes = {0xda, 0x3d, 0xa4, 0xad, 0x43, 0x81, 0xdd, 0x86, 0x09, 0xcd, 0x2b, 0xba, 0xf5, 0x6f, 0x00, 0x89, 0xcd, 0x5d, 0x53, 0x99, 0x2a, 0x80, 0x73, 0xf9, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
				BigInteger rateNumerator = rateNumeratorBytes.AsBigInteger();
				amount = (amount * rateNumerator) / rateDenominator;
				deltaTime -= (1 << 12);
			}
			if((deltaTime >> 11) > 0){
				byte[] rateNumeratorBytes = {0x05, 0x47, 0x65, 0x80, 0x48, 0x59, 0x93, 0x6e, 0x51, 0x04, 0x2c, 0x10, 0x39, 0x4f, 0x6e, 0xa4, 0xc0, 0x61, 0xd9, 0xd7, 0xb8, 0xba, 0xb9, 0xfc, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
				BigInteger rateNumerator = rateNumeratorBytes.AsBigInteger();
				amount = (amount * rateNumerator) / rateDenominator;
				deltaTime -= (1 << 11);
			}
			if((deltaTime >> 10) > 0){
				byte[] rateNumeratorBytes = {0x02, 0x8a, 0x45, 0xee, 0x8d, 0xb5, 0x81, 0x17, 0x09, 0x21, 0x12, 0x01, 0x8e, 0x51, 0x30, 0xb7, 0xdf, 0xd5, 0x86, 0x4c, 0x05, 0xdc, 0x5c, 0xfe, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
				BigInteger rateNumerator = rateNumeratorBytes.AsBigInteger();
				amount = (amount * rateNumerator) / rateDenominator;
				deltaTime -= (1 << 10);
			}
			if((deltaTime >> 9) > 0){
				byte[] rateNumeratorBytes = {0x22, 0x6c, 0xa2, 0xae, 0xc2, 0xe4, 0xcc, 0x0e, 0x76, 0xe7, 0xa5, 0xc6, 0xd8, 0x4f, 0x03, 0x38, 0xd4, 0xba, 0x23, 0xde, 0xac, 0x6d, 0x2e, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
				BigInteger rateNumerator = rateNumeratorBytes.AsBigInteger();
				amount = (amount * rateNumerator) / rateDenominator;
				deltaTime -= (1 << 9);
			}
			if((deltaTime >> 8) > 0){
				byte[] rateNumeratorBytes = {0xef, 0x35, 0x97, 0x2b, 0xb7, 0xf5, 0x3d, 0xb7, 0xc2, 0xdd, 0xe6, 0x7c, 0x28, 0x27, 0xd3, 0x79, 0x31, 0x2a, 0x01, 0xfd, 0xc0, 0x36, 0x97, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
				BigInteger rateNumerator = rateNumeratorBytes.AsBigInteger();
				amount = (amount * rateNumerator) / rateDenominator;
				deltaTime -= (1 << 8);
			}
			if((deltaTime >> 7) > 0){
				byte[] rateNumeratorBytes = {0x5d, 0x5f, 0xa7, 0xd0, 0xae, 0xe8, 0x78, 0xce, 0xaa, 0x92, 0x7d, 0x67, 0xeb, 0x9d, 0x99, 0xf8, 0x63, 0x4f, 0xfb, 0x21, 0x5b, 0x9b, 0xcb, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
				BigInteger rateNumerator = rateNumeratorBytes.AsBigInteger();
				amount = (amount * rateNumerator) / rateDenominator;
				deltaTime -= (1 << 7);
			}
			if((deltaTime >> 6) > 0){
				byte[] rateNumeratorBytes = {0x2b, 0x20, 0xc7, 0xf3, 0xc3, 0xcc, 0xd8, 0xc2, 0x5f, 0x80, 0x1a, 0xeb, 0x2b, 0x92, 0x76, 0xf1, 0x27, 0x33, 0xdc, 0x39, 0xac, 0xcd, 0xe5, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
				BigInteger rateNumerator = rateNumeratorBytes.AsBigInteger();
				amount = (amount * rateNumerator) / rateDenominator;
				deltaTime -= (1 << 6);
			}
			if((deltaTime >> 5) > 0){
				byte[] rateNumeratorBytes = {0xc8, 0x8f, 0xe1, 0x25, 0xa2, 0x5f, 0xea, 0x4a, 0xaf, 0xd9, 0x3e, 0x16, 0x9d, 0xd6, 0x68, 0xda, 0x0d, 0xb8, 0x25, 0xc7, 0xd5, 0xe6, 0xf2, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
				BigInteger rateNumerator = rateNumeratorBytes.AsBigInteger();
				amount = (amount * rateNumerator) / rateDenominator;
				deltaTime -= (1 << 5);
			}
			if((deltaTime >> 4) > 0){
				byte[] rateNumeratorBytes = {0xf6, 0xac, 0xe0, 0x6a, 0xa3, 0x5b, 0xc1, 0xbd, 0xa5, 0xea, 0x6f, 0x62, 0x91, 0xe8, 0x25, 0xf2, 0x18, 0xc3, 0x20, 0xce, 0x6a, 0x73, 0xf9, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
				BigInteger rateNumerator = rateNumeratorBytes.AsBigInteger();
				amount = (amount * rateNumerator) / rateDenominator;
				deltaTime -= (1 << 4);
			}
			if((deltaTime >> 3) > 0){
				byte[] rateNumeratorBytes = {0x53, 0xc2, 0x3b, 0x5e, 0x8a, 0x55, 0xd4, 0xfd, 0xde, 0xcd, 0xe0, 0x22, 0x3a, 0xb2, 0xdf, 0x6b, 0x3f, 0xdb, 0xb3, 0x61, 0xb5, 0xb9, 0xfc, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
				BigInteger rateNumerator = rateNumeratorBytes.AsBigInteger();
				amount = (amount * rateNumerator) / rateDenominator;
				deltaTime -= (1 << 3);
			}
			if((deltaTime >> 2) > 0){
				byte[] rateNumeratorBytes = {0x8c, 0x0a, 0x0d, 0x62, 0xe5, 0x6c, 0xc7, 0x9c, 0xe7, 0x54, 0xd5, 0xce, 0x23, 0x0e, 0xd5, 0x40, 0x0a, 0xcc, 0x82, 0xaf, 0xda, 0x5c, 0xfe, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
				BigInteger rateNumerator = rateNumeratorBytes.AsBigInteger();
				amount = (amount * rateNumerator) / rateDenominator;
				deltaTime -= (1 << 2);
			}
			if((deltaTime >> 1) > 0){
				byte[] rateNumeratorBytes = {0xbb, 0xf7, 0x84, 0x26, 0xf1, 0xd1, 0x69, 0x0f, 0xe2, 0x47, 0x7a, 0x47, 0xa0, 0x14, 0xea, 0x7c, 0x9f, 0x9d, 0x6b, 0x57, 0x6d, 0x2e, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
				BigInteger rateNumerator = rateNumeratorBytes.AsBigInteger();
				amount = (amount * rateNumerator) / rateDenominator;
				deltaTime -= (1 << 1);
			}

			amount = max(amount, 1); //Needed?
			return amount;
		}

		private static Balance updateBalance(Balance bal){
			BigInteger currentTime = Runtime.Time;
			bal.amount = updateAmount(bal.amount, bal.lastTimeTransfered, currentTime);
			bal.lastTimeTransfered = currentTime;
			return bal;
		}

        [DisplayName("balanceOf")]
        public static BigInteger BalanceOf(byte[] account)
        {
            if (account.Length != 20)
                throw new InvalidOperationException("The parameter account SHOULD be 20-byte addresses.");
            StorageMap asset = Storage.CurrentContext.CreateMap(nameof(asset));
            Balance bal = deserializeBalance(asset.Get(account));
			return updateAmount(bal.amount, bal.lastTimeTransfered, Runtime.Time);
        }

        [DisplayName("decimals")]
        public static byte Decimals() => 8;

        private static bool IsPayable(byte[] to)
        {
            var c = Blockchain.GetContract(to);
            return c == null || c.IsPayable;
        }

        [DisplayName("name")]
        public static string Name() => "GinoMo"; //name of the token

        [DisplayName("symbol")]
        public static string Symbol() => "GM"; //symbol of the token

        [DisplayName("supportedStandards")]
        public static string[] SupportedStandards() => new string[] { "NEP-5", "NEP-7", "NEP-10" };

        [DisplayName("totalSupply")]
        public static BigInteger TotalSupply()
        {
            StorageMap contract = Storage.CurrentContext.CreateMap(nameof(contract));
            return contract.Get("totalSupply").AsBigInteger();
        }
#if DEBUG
        [DisplayName("transfer")] //Only for ABI file
        public static bool Transfer(byte[] from, byte[] to, BigInteger amount) => true;
#endif
        //Methods of actual execution
        private static bool Transfer(byte[] from, byte[] to, BigInteger amount, byte[] callscript)
        {
            //Check parameters
            if (from.Length != 20 || to.Length != 20)
                throw new InvalidOperationException("The parameters from and to SHOULD be 20-byte addresses.");
            if (amount <= 0)
                throw new InvalidOperationException("The parameter amount MUST be greater than 0.");
            if (!IsPayable(to))
                return false;
            if (!Runtime.CheckWitness(from) && from.AsBigInteger() != callscript.AsBigInteger())
                return false;
            StorageMap asset = Storage.CurrentContext.CreateMap(nameof(asset));
            Balance fromBalance = updateBalance(deserializeBalance(asset.Get(from)));
            if (fromBalance.amount < amount)
                return false;
            if (from == to)
                return true;

            //Reduce payer balances
            if (fromBalance.amount == amount){
                asset.Delete(from);
			} else {
				fromBalance.amount -= amount;
                asset.Put(from, Helper.Serialize(fromBalance));
			}

            //Increase the payee balance
            Balance toBalance = updateBalance(deserializeBalance(asset.Get(to)));
			toBalance.amount += amount;
            asset.Put(to, Helper.Serialize(toBalance));

            Transferred(from, to, amount);
            return true;
        }
    }
}
