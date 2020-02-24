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

		private static BigInteger updateAmount(BigInteger oldAmount, BigInteger lastTransfer, BigInteger currentTime){
			BigInteger rateNumerator = 218378756114605; //Corresponds to 5% per year
			BigInteger rateDenominator = 10**23;
			BigInteger deltaTime = currentTime - lastTransfer;
			BigInteger newAmount = oldAmount - (oldAmount*deltaTime*rateNumerator)/rateDenominator; //Important to make sure that we don't reach overflow here
			newAmount = max(newAmount, 0); //Make sure we don't hit negative numbers
			return newAmount;
		}

		private static Balance updateBalance(Balance bal){
			BigInteger currentTime = Runtime.Time;
			fromBalance.amount = updateAmount(bal.amount, bal.lastTimeTransfered, currentTime);
			bal.lastTimeTransfered = currentTime;
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

			//Update total supply?

            Transferred(from, to, amount);
            return true;
        }
    }
}
