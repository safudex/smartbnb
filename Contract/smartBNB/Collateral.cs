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
		[DisplayName("deposited")]
		public static event Action<byte[], byte[], BigInteger> Deposited;

		// General spec: https://docs.neo.org/tutorial/en-us/9-smartContract/cgas/1_what_is_cgas.html
		// Contract addresses: https://medium.com/neo-smart-economy/15-things-you-should-know-about-cneo-and-cgas-1029770d76e0
		// https://github.com/neo-ngd/CNEO-Contract
		// https://github.com/neo-ngd/CGAS-Contract
		private static readonly byte[] CGAS = "AScKxyXmNtEnTLTvbVhNQyTJmgytxhwSnM".ToScriptHash();
		private static readonly byte[] CNEO = "AQbg4gk1Q6FaGCtfEKu2ETSMP6U25YDVR3".ToScriptHash();

		public static object Main(string method, object[] args)
		{
			if (Runtime.Trigger == TriggerType.Application)
			{

				if (method == "deposit") // (originator, assetID, amount)
				{
					if (args.Length != 3) return false;
					return Deposit((byte[])args[0], (byte[])args[1], (bool)args[2]);
				}
			}
		}

		private static void TransferNEP5(byte[] from, byte[] to, byte[] assetID, BigInteger amount)
		{
			// Transfer token
			var args = new object[] { from, to, amount };
			var contract = (NEP5Contract)assetID.ToDelegate();
			if (!(bool)contract("transfer", args)) throw new Exception("Failed to transfer NEP-5 tokens!");
		}

		private static bool ReceivedNEP5(byte[] originator, byte[] assetID, BigInteger amount)
		{
			// Verify that deposit is authorized
			if (!Runtime.CheckWitness(originator)) return false;
			// Check amount
			if (amount < 1) return false;
			// Update balances first
			BigInteger balance = asset.Get(originator).AsBigInteger();
            balance += amount;
            asset.Put(originator, balance);

            Deposited(originator, assetID, amount);

            return true;
        }

		// This function and the functions being called by it were taken from the switcheo contract
		// https://github.com/Switcheo/switcheo-neo/blob/master/switcheo/BrokerContract.cs
		private static bool Deposit(byte[] originator, bool assetNeo, BigInteger amount)
		{
			byte[] assetID = (assetNeo == true)? CNEO : CGAS;

			// Update balances first
			if (!ReceivedNEP5(originator, assetID, amount)) return false;

			// Execute deposit to our contract (ExecutionEngine.ExecutingScriptHash)
			TransferNEP5(originator, ExecutionEngine.ExecutingScriptHash, assetID, amount);

			return true;
		}
	}
}

