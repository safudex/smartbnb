using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;
using Helper = Neo.SmartContract.Framework.Helper;

namespace smartBNB
{
    public class Contract : SmartContract
    {
        private static readonly byte CONTRACT_STATUS_PORTREQUEST = 0x01;//WAITING FOR THE USER TO SEND BNB
        private static readonly byte CONTRACT_STATUS_DEPOSITOK = 0x02;
        private static readonly byte CONTRACT_STATUS_WITHDRAWREQUESTED = 0x03;
        private static readonly byte CONTRACT_STATUS_CHALLENGEDEPOSIT = 0x04;//CHALLENGE ACTIVATED
        private static readonly byte CONTRACT_STATUS_CHALLENGEWITHDRAW = 0x05;//CHALLENGE ACTIVATED
        private static readonly byte CONTRACT_STATUS_CHALLENGEDEPOSIT_FAILED = 0x06;
        private static readonly byte CONTRACT_STATUS_CHALLENGEWITHDRAW_FAILED = 0x07;

        // TODO: Update these constants
        private static readonly BigInteger CONTRACT_TIMEOUT_PORTREQUEST = 0;//60*60*12;
        private static readonly BigInteger CONTRACT_TIMEOUT_UPLOADPROOF = 60*5;//60*60*12;
        private static readonly BigInteger CONTRACT_TIMEOUT_WITHDRAWREQUEST = 60*5;//60*60*12;
        private static readonly BigInteger WINDOW_CHALLENGE = 60*5;//60*60*12;

        private static readonly BigInteger DEPOSIT_CHALLENGE = 130;

        private static readonly BigInteger FACTOR_COLLATERAL_NUMERATOR = 3;
        private static readonly BigInteger FACTOR_COLLATERAL_DENOMINATOR = 2;
        private static readonly BigInteger PRICE_DENOMINATOR = 1000000;
        private static readonly BigInteger FACTOR_PORTREQUEST_DIVISOR = 10;

        private static readonly byte OPERATION_ADD = 0x01;//WAITING FOR THE USER TO SEND BNB
        private static readonly byte OPERATION_SUB = 0x02;//CHALLENGE ACTIVATED

        // See https://docs.tendermint.com/master/spec/blockchain/encoding.html#merkle-trees
        private static readonly byte[] leafPrefix = { 0x00 };
        private static readonly byte[] innerPrefix = { 0x01 };
        private static readonly int slicesLen = 128;

        private static readonly byte STG_GENERAL = 0x01;
        private static readonly byte STG_POINTMUL = 0x02;

        private static readonly byte[] byteP =  {0xed, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x7f};

        private static readonly byte[] byteD = {0xa3, 0x78, 0x59, 0x13, 0xca, 0x4d, 0xeb, 0x75, 0xab, 0xd8, 0x41, 0x41, 0x4d, 0x0a, 0x70, 0x00, 0x98, 0xe8, 0x79, 0x77, 0x79, 0x40, 0xc7, 0x8c, 0x73, 0xfe, 0x6f, 0x2b, 0xee, 0x6c, 0x03, 0x52};

        // Static call docs: https://docs.neo.org/docs/en-us/sc/deploy/invoke.html
        // General spec: https://docs.neo.org/tutorial/en-us/9-smartContract/cgas/1_what_is_cgas.html
        // Contract addresses: https://medium.com/neo-smart-economy/15-things-you-should-know-about-cneo-and-cgas-1029770d76e0
        // Code: https://github.com/neo-ngd/CGAS-Contract
        [Appcall("74f2dc36a68fdc4682034178eb2220729231db76")] // ScriptHash of CGAS (address: AScKxyXmNtEnTLTvbVhNQyTJmgytxhwSnM)
        public static extern object CGAS(string method, object[] args);

        private static readonly byte[] PriceOracle = "ALfnhLg7rUyL6Jr98bzzoxz5J7m64fbR4s".ToScriptHash(); // TODO: Update

        [DisplayName("deposited")]
        public static event Action<byte[], BigInteger> Deposited;

        [DisplayName("transfer")]
        public static event Action<byte[], byte[], BigInteger> Transferred;

        [Serializable]
        struct Balance
        {
            public BigInteger amount;
            public BigInteger lastTimeTransfered;
        }

        [Serializable]
        public struct PortingContract
        {
            public byte ContractStatus;
            public byte[] CollatBCNAddr;
            public byte[] CollatAddr;
            public byte[] UserAddr;
            public BigInteger AmountBNB;
            public BigInteger LastTimestamp;
        }

        [Serializable]
        public struct Collat
        {
            public byte[] Address;
            public byte[] BNCAddress;
            public BigInteger CollateralAmount;
            public BigInteger CustodiedBNB;
        }

        [Serializable]
        struct GeneralChallengeVariables
        {
            public byte[][] signature;
            public BigInteger[] xs;
            public BigInteger[] ys;
            public byte[][] signableBytes;
            public ulong[][] pre;
            public ulong[][] preHash;
            public BigInteger[] preHashMod;
            public byte[] txproof;
            public byte[] blockHeader;
            public ulong[] txBytes;
        }

        [Serializable]
        struct PointMulStep
        {
            public BigInteger[] Q;
            public BigInteger s;
            public BigInteger[] P;
        }

        public static bool Main(string operation, params object[] args)
        {
            Storage.Put("exec", operation); //DEBUG

            //[pubk0, pubk1, pubk2, ..., pubk10]
            byte[][] pubks = new byte[][] {
                new byte[]{0xd3, 0x76, 0x9d, 0x8a, 0x1f, 0x78, 0xb4, 0xc1, 0x7a, 0x96, 0x5f, 0x7a, 0x30, 0xd4, 0x18, 0x1f, 0xab, 0xbd, 0x1f, 0x96, 0x9f, 0x46, 0xd3, 0xc8, 0xe8, 0x3b, 0x5a, 0xd4, 0x84, 0x54, 0x21, 0xd8},
                new byte[]{0x2b, 0xa4, 0xe8, 0x15, 0x42, 0xf4, 0x37, 0xb7, 0xae, 0x1f, 0x8a, 0x35, 0xdd, 0xb2, 0x33, 0xc7, 0x89, 0xa8, 0xdc, 0x22, 0x73, 0x43, 0x77, 0xd9, 0xb6, 0xd6, 0x3a, 0xf1, 0xca, 0x40, 0x3b, 0x61},
                new byte[]{0xdf, 0x8d, 0xa8, 0xc5, 0xab, 0xfd, 0xb3, 0x85, 0x95, 0x39, 0x13, 0x08, 0xbb, 0x71, 0xe5, 0xa1, 0xe0, 0xaa, 0xbd, 0xc1, 0xd0, 0xcf, 0x38, 0x31, 0x5d, 0x50, 0xd6, 0xbe, 0x93, 0x9b, 0x26, 0x06},
                new byte[]{0xb6, 0x61, 0x9e, 0xdc, 0xa4, 0x14, 0x34, 0x84, 0x80, 0x02, 0x81, 0xd6, 0x98, 0xb7, 0x0c, 0x93, 0x5e, 0x91, 0x52, 0xad, 0x57, 0xb3, 0x1d, 0x85, 0xc0, 0x5f, 0x2f, 0x79, 0xf6, 0x4b, 0x39, 0xf3},
                new byte[]{0x94, 0x46, 0xd1, 0x4a, 0xd8, 0x6c, 0x8d, 0x2d, 0x74, 0x78, 0x0b, 0x08, 0x47, 0x11, 0x00, 0x01, 0xa1, 0xc2, 0xe2, 0x52, 0xee, 0xdf, 0xea, 0x47, 0x53, 0xeb, 0xbb, 0xfc, 0xe3, 0xa2, 0x2f, 0x52},
                new byte[]{0x03, 0x53, 0xc6, 0x39, 0xf8, 0x0c, 0xc8, 0x01, 0x59, 0x44, 0x43, 0x6d, 0xab, 0x10, 0x32, 0x24, 0x5d, 0x44, 0xf9, 0x12, 0xed, 0xc3, 0x1e, 0xf6, 0x68, 0xff, 0x9f, 0x4a, 0x45, 0xcd, 0x05, 0x99},
                new byte[]{0xe8, 0x1d, 0x37, 0x97, 0xe0, 0x54, 0x4c, 0x3a, 0x71, 0x8e, 0x1f, 0x05, 0xf0, 0xfb, 0x78, 0x22, 0x12, 0xe2, 0x48, 0xe7, 0x84, 0xc1, 0xa8, 0x51, 0xbe, 0x87, 0xe7, 0x7a, 0xe0, 0xdb, 0x23, 0x0e},
                new byte[]{0x5e, 0x3f, 0xcd, 0xa3, 0x0b, 0xd1, 0x9d, 0x45, 0xc4, 0xb7, 0x36, 0x88, 0xda, 0x35, 0xe7, 0xda, 0x1f, 0xce, 0x7c, 0x68, 0x59, 0xb2, 0xc1, 0xf2, 0x0e, 0xd5, 0x20, 0x2d, 0x24, 0x14, 0x4e, 0x3e},
                new byte[]{0xb0, 0x6a, 0x59, 0xa2, 0xd7, 0x5b, 0xf5, 0xd0, 0x14, 0xfc, 0xe7, 0xc9, 0x99, 0xb5, 0xe7, 0x1e, 0x7a, 0x96, 0x08, 0x70, 0xf7, 0x25, 0x84, 0x7d, 0x4b, 0xa3, 0x23, 0x5b, 0xae, 0xaa, 0x08, 0xef},
                new byte[]{0x0c, 0x91, 0x0e, 0x2f, 0xe6, 0x50, 0xe4, 0xe0, 0x14, 0x06, 0xb3, 0x31, 0x0b, 0x48, 0x9f, 0xb6, 0x0a, 0x84, 0xbc, 0x3f, 0xf5, 0xc5, 0xbe, 0xe3, 0xa5, 0x6d, 0x58, 0x98, 0xb6, 0xa8, 0xaf, 0x32},
                new byte[]{0x71, 0xf2, 0xd7, 0xb8, 0xec, 0x1c, 0x8b, 0x99, 0xa6, 0x53, 0x42, 0x9b, 0x01, 0x18, 0xcd, 0x20, 0x1f, 0x79, 0x4f, 0x40, 0x9d, 0x0f, 0xea, 0x4d, 0x65, 0xb1, 0xb6, 0x62, 0xf2, 0xb0, 0x00, 0x63}
            };

            //[X0, Y0, X0Y0%P, X1, Y1, X1Y1%P, ...]
            byte[][] pubksDecompressed = new byte[][] {
                new byte[] {0x6b, 0xef, 0x5e, 0xac, 0x02, 0xc3, 0x50, 0x83, 0x30, 0x3a, 0xf1, 0x52, 0xbb, 0xa1, 0xda, 0x6d, 0x41, 0xe4, 0xda, 0xfe, 0xa9, 0x2d, 0x9e, 0x3a, 0x78, 0x8f, 0x6a, 0x57, 0x12, 0xc7, 0x5e, 0x0d},
                new byte[] {0xd3, 0x76, 0x9d, 0x8a, 0x1f, 0x78, 0xb4, 0xc1, 0x7a, 0x96, 0x5f, 0x7a, 0x30, 0xd4, 0x18, 0x1f, 0xab, 0xbd, 0x1f, 0x96, 0x9f, 0x46, 0xd3, 0xc8, 0xe8, 0x3b, 0x5a, 0xd4, 0x84, 0x54, 0x21, 0x58},
                new byte[] {0xca, 0xfd, 0xf9, 0x28, 0x00, 0x57, 0x8b, 0x44, 0x38, 0x4d, 0x96, 0x5a, 0x9e, 0x50, 0x39, 0x59, 0xb5, 0xf9, 0x62, 0x65, 0x7b, 0xfe, 0x5c, 0x76, 0x76, 0x7b, 0x38, 0xfa, 0x6a, 0x8d, 0x60, 0x68},
                new byte[] {0xf0, 0x26, 0xf7, 0x0e, 0xcb, 0xad, 0xed, 0xe0, 0x25, 0xda, 0x0b, 0x8a, 0x32, 0x22, 0xa7, 0x4b, 0x97, 0x38, 0xa2, 0xc0, 0xd0, 0x0e, 0xd2, 0xac, 0xd3, 0x25, 0xb2, 0x2e, 0x88, 0xe8, 0xcf, 0x1a},
                new byte[] {0x2b, 0xa4, 0xe8, 0x15, 0x42, 0xf4, 0x37, 0xb7, 0xae, 0x1f, 0x8a, 0x35, 0xdd, 0xb2, 0x33, 0xc7, 0x89, 0xa8, 0xdc, 0x22, 0x73, 0x43, 0x77, 0xd9, 0xb6, 0xd6, 0x3a, 0xf1, 0xca, 0x40, 0x3b, 0x61},
                new byte[] {0xa2, 0xa2, 0x21, 0xbb, 0xb0, 0x85, 0xd8, 0xf5, 0x22, 0x7d, 0x62, 0xff, 0x38, 0x2f, 0xbd, 0x6d, 0xc0, 0xa3, 0xec, 0xcc, 0xaa, 0xfa, 0x5c, 0x95, 0xee, 0xc3, 0xcb, 0x3c, 0xfd, 0xea, 0x2d, 0x00},
                new byte[] {0x52, 0x91, 0xa8, 0x7d, 0x92, 0x12, 0x01, 0x90, 0xbb, 0xd3, 0xd2, 0x61, 0x92, 0x18, 0x1f, 0xe7, 0x31, 0x6b, 0x1a, 0x05, 0xaa, 0x96, 0xb1, 0x57, 0xe3, 0xfa, 0x4a, 0x32, 0x59, 0x1c, 0xcc, 0x26},
                new byte[] {0xdf, 0x8d, 0xa8, 0xc5, 0xab, 0xfd, 0xb3, 0x85, 0x95, 0x39, 0x13, 0x08, 0xbb, 0x71, 0xe5, 0xa1, 0xe0, 0xaa, 0xbd, 0xc1, 0xd0, 0xcf, 0x38, 0x31, 0x5d, 0x50, 0xd6, 0xbe, 0x93, 0x9b, 0x26, 0x06},
                new byte[] {0xef, 0x51, 0xe6, 0xb9, 0x5c, 0x30, 0x33, 0x2d, 0x9d, 0x11, 0x32, 0x6f, 0x27, 0x70, 0xd9, 0x98, 0x0b, 0x89, 0x0d, 0xf3, 0xc4, 0x30, 0x1d, 0x96, 0x85, 0xe9, 0xa9, 0xba, 0x98, 0xec, 0x3a, 0x3b},
                new byte[] {0xc1, 0xd0, 0xea, 0xc9, 0x42, 0x3e, 0xe0, 0xc0, 0x63, 0x7f, 0x11, 0x19, 0xfd, 0xbb, 0xd6, 0x41, 0x83, 0xbd, 0x64, 0x2d, 0xb8, 0x16, 0x0b, 0xcd, 0xb1, 0x40, 0x2c, 0x62, 0xc9, 0x1f, 0x1b, 0x21},
                new byte[] {0xb6, 0x61, 0x9e, 0xdc, 0xa4, 0x14, 0x34, 0x84, 0x80, 0x02, 0x81, 0xd6, 0x98, 0xb7, 0x0c, 0x93, 0x5e, 0x91, 0x52, 0xad, 0x57, 0xb3, 0x1d, 0x85, 0xc0, 0x5f, 0x2f, 0x79, 0xf6, 0x4b, 0x39, 0x73},
                new byte[] {0x70, 0xa5, 0xe2, 0xc7, 0x04, 0x71, 0x5e, 0x72, 0x65, 0x6f, 0x7b, 0xce, 0xdb, 0xa8, 0x18, 0x29, 0xcd, 0x67, 0x3d, 0x77, 0x28, 0x0f, 0xc1, 0xf0, 0x5b, 0xfc, 0x77, 0xe3, 0xcd, 0xbf, 0xcd, 0x0d},
                new byte[] {0x46, 0xee, 0x2f, 0x7a, 0x32, 0xf6, 0x22, 0x21, 0xd6, 0xce, 0x2a, 0xcb, 0xbe, 0x3d, 0xd5, 0x86, 0x59, 0x51, 0x3b, 0x55, 0xad, 0xed, 0xf1, 0x49, 0xfd, 0x6f, 0x7e, 0x3c, 0x18, 0x9f, 0x05, 0x68},
                new byte[] {0x94, 0x46, 0xd1, 0x4a, 0xd8, 0x6c, 0x8d, 0x2d, 0x74, 0x78, 0x0b, 0x08, 0x47, 0x11, 0x00, 0x01, 0xa1, 0xc2, 0xe2, 0x52, 0xee, 0xdf, 0xea, 0x47, 0x53, 0xeb, 0xbb, 0xfc, 0xe3, 0xa2, 0x2f, 0x52},
                new byte[] {0x00, 0xfb, 0x75, 0x06, 0x32, 0x14, 0xaa, 0xb4, 0x36, 0x2f, 0xce, 0xb8, 0x92, 0x8f, 0x6c, 0x63, 0x0d, 0x96, 0x04, 0x05, 0x88, 0xc4, 0x0f, 0x71, 0xea, 0x33, 0x81, 0xb9, 0xe2, 0x5f, 0xec, 0x6c},
                new byte[] {0x41, 0xee, 0x23, 0x13, 0xc4, 0xdc, 0x50, 0xfb, 0x95, 0x53, 0x65, 0xff, 0x9d, 0xd9, 0xbb, 0xb9, 0x6e, 0xa0, 0x9e, 0x5e, 0xf4, 0xd6, 0x05, 0xb5, 0x8a, 0xa2, 0x29, 0x38, 0x69, 0xbf, 0x22, 0x3e},
                new byte[] {0x03, 0x53, 0xc6, 0x39, 0xf8, 0x0c, 0xc8, 0x01, 0x59, 0x44, 0x43, 0x6d, 0xab, 0x10, 0x32, 0x24, 0x5d, 0x44, 0xf9, 0x12, 0xed, 0xc3, 0x1e, 0xf6, 0x68, 0xff, 0x9f, 0x4a, 0x45, 0xcd, 0x05, 0x19},
                new byte[] {0xaf, 0xac, 0x0f, 0x35, 0xb9, 0xd3, 0xe8, 0x92, 0x61, 0x1c, 0xcd, 0xef, 0xe3, 0x75, 0x41, 0xf5, 0xd4, 0x35, 0x66, 0x5f, 0x05, 0x2d, 0x97, 0x99, 0x4f, 0xc6, 0xfa, 0x5f, 0x3d, 0x89, 0x4f, 0x20},
                new byte[] {0x00, 0x39, 0x38, 0x85, 0xb6, 0x7a, 0x80, 0x4d, 0x83, 0x89, 0xec, 0x51, 0x65, 0x24, 0x9e, 0x59, 0x5d, 0xa5, 0x1b, 0x38, 0x9c, 0x26, 0xba, 0x9f, 0x69, 0x15, 0x1b, 0x4f, 0xc3, 0x78, 0x1e, 0x56},
                new byte[] {0xe8, 0x1d, 0x37, 0x97, 0xe0, 0x54, 0x4c, 0x3a, 0x71, 0x8e, 0x1f, 0x05, 0xf0, 0xfb, 0x78, 0x22, 0x12, 0xe2, 0x48, 0xe7, 0x84, 0xc1, 0xa8, 0x51, 0xbe, 0x87, 0xe7, 0x7a, 0xe0, 0xdb, 0x23, 0x0e},
                new byte[] {0xc6, 0x96, 0x78, 0x8e, 0x22, 0xcd, 0xa7, 0x13, 0x22, 0xbf, 0x7e, 0xaf, 0xe1, 0x61, 0x7a, 0x28, 0x6b, 0x3f, 0x16, 0xa5, 0xdc, 0x86, 0x69, 0x47, 0x67, 0x65, 0x51, 0x21, 0x38, 0xf0, 0x74, 0x38},
                new byte[] {0x68, 0xc7, 0xd9, 0xd4, 0x0c, 0xd2, 0x6d, 0xe1, 0x91, 0x17, 0x3c, 0xd0, 0xfd, 0xe4, 0x3a, 0xce, 0xf8, 0x18, 0x01, 0xe8, 0xbf, 0x90, 0x1b, 0xa0, 0x52, 0xc1, 0x92, 0x15, 0xfb, 0x7b, 0x3d, 0x08},
                new byte[] {0x5e, 0x3f, 0xcd, 0xa3, 0x0b, 0xd1, 0x9d, 0x45, 0xc4, 0xb7, 0x36, 0x88, 0xda, 0x35, 0xe7, 0xda, 0x1f, 0xce, 0x7c, 0x68, 0x59, 0xb2, 0xc1, 0xf2, 0x0e, 0xd5, 0x20, 0x2d, 0x24, 0x14, 0x4e, 0x3e},
                new byte[] {0x96, 0x14, 0x7e, 0x04, 0x6d, 0x93, 0x96, 0x3f, 0xab, 0xd2, 0xa3, 0xc9, 0x8d, 0x54, 0xc5, 0xdd, 0xcf, 0xf9, 0x23, 0x33, 0x9c, 0x85, 0xca, 0x03, 0xe7, 0xe0, 0xab, 0x4b, 0x4d, 0x2b, 0x94, 0x7e},
                new byte[] {0x29, 0x70, 0xb0, 0xe3, 0xfd, 0xe7, 0x0a, 0x73, 0xfd, 0xd7, 0x67, 0xa8, 0x49, 0x29, 0x4e, 0x78, 0xc4, 0x08, 0xf3, 0x01, 0x63, 0xce, 0xaf, 0x1a, 0x35, 0x28, 0x30, 0xf6, 0xf7, 0xcb, 0xe9, 0x23},
                new byte[] {0xb0, 0x6a, 0x59, 0xa2, 0xd7, 0x5b, 0xf5, 0xd0, 0x14, 0xfc, 0xe7, 0xc9, 0x99, 0xb5, 0xe7, 0x1e, 0x7a, 0x96, 0x08, 0x70, 0xf7, 0x25, 0x84, 0x7d, 0x4b, 0xa3, 0x23, 0x5b, 0xae, 0xaa, 0x08, 0x6f},
                new byte[] {0x64, 0xbb, 0x01, 0xba, 0x36, 0x7e, 0x64, 0xe3, 0x3e, 0xe3, 0x37, 0x1d, 0xcf, 0x92, 0xe2, 0x80, 0xc1, 0x3b, 0x9b, 0x56, 0xe6, 0xbe, 0x88, 0x19, 0x19, 0xed, 0x42, 0x11, 0x53, 0x85, 0xdd, 0x44},
                new byte[] {0x4e, 0x13, 0xe1, 0x7c, 0x14, 0xc2, 0x77, 0x0b, 0xf1, 0xa2, 0x4c, 0x07, 0x0e, 0x25, 0x30, 0x3e, 0xa4, 0xd4, 0x17, 0xb7, 0x76, 0x99, 0xe0, 0x0b, 0xf3, 0x27, 0xaf, 0x28, 0xf1, 0xea, 0x90, 0x63},
                new byte[] {0x0c, 0x91, 0x0e, 0x2f, 0xe6, 0x50, 0xe4, 0xe0, 0x14, 0x06, 0xb3, 0x31, 0x0b, 0x48, 0x9f, 0xb6, 0x0a, 0x84, 0xbc, 0x3f, 0xf5, 0xc5, 0xbe, 0xe3, 0xa5, 0x6d, 0x58, 0x98, 0xb6, 0xa8, 0xaf, 0x32},
                new byte[] {0x57, 0x58, 0xeb, 0xe4, 0x96, 0x8d, 0x04, 0xce, 0x9d, 0x13, 0xf1, 0xe3, 0x86, 0x09, 0xa2, 0x35, 0xe0, 0x40, 0xbd, 0xcb, 0xb1, 0x47, 0x1a, 0x35, 0x87, 0x72, 0xbc, 0x45, 0xdc, 0x7c, 0x7d, 0x30},
                new byte[] {0x24, 0xac, 0xfa, 0xa3, 0x92, 0x99, 0xac, 0x93, 0x3b, 0x77, 0x40, 0x12, 0x77, 0xb0, 0x97, 0x34, 0x15, 0xf2, 0xf5, 0xe9, 0x8a, 0xda, 0xa1, 0x3f, 0x8a, 0x9f, 0x6e, 0xf8, 0xe4, 0x7e, 0xa2, 0x0a},
                new byte[] {0x71, 0xf2, 0xd7, 0xb8, 0xec, 0x1c, 0x8b, 0x99, 0xa6, 0x53, 0x42, 0x9b, 0x01, 0x18, 0xcd, 0x20, 0x1f, 0x79, 0x4f, 0x40, 0x9d, 0x0f, 0xea, 0x4d, 0x65, 0xb1, 0xb6, 0x62, 0xf2, 0xb0, 0x00, 0x63},
                new byte[] {0x50, 0x4c, 0xd0, 0x7c, 0x07, 0xf7, 0x27, 0xd4, 0x71, 0xc4, 0x2b, 0x42, 0x13, 0x0c, 0xcf, 0x4c, 0x2f, 0x86, 0xb2, 0xe6, 0xe9, 0x44, 0x12, 0xfe, 0x89, 0x84, 0xf8, 0x47, 0x32, 0x01, 0xef, 0x72}
            };


            if (Runtime.Trigger == TriggerType.Application)
            {
                if(operation=="savestate")
                {
                    /* ARGS
                       for general state:
                       0 byte[] portingContractID
                       1 byte[][] signatures
                       2 bigint[] xs (decompressed signature x)
                       3 bigint[] ys (decompressed signature y)
                       4 byte[][] signablebytes
                       5 ulong[][] pres
                       6 ulong[][] preshash
                       7 bigint[] preshashmod
                       8 byte[] txproof
                       90 byte[] blockHeader
                       01 byte[] txBytes

                       for point mul
                       0 byte[] collatid
                       1 byte[] txid
                       2 string type (simple/multi -> BigInteger[]/BigInteger[][])
                       3 string id (pointmulid+slice num)
                       4 BigInteger[]/BigInteger[][] data
                       */
                    bool r = SaveChallengeState(args);
                    return r;
                }
                else if (operation=="executeChallenge")
                {
                    bool r = executeChallenge((byte[])args[0], (byte)args[1], (int)args[1], pubks, pubksDecompressed, args);
                    return r;
                }
                else if (operation=="registerAsCollateral")
                {
                    bool r = RegisterAsCollateral((byte[])args[0], (byte[])args[1], (BigInteger)args[2], (byte)args[3]);
                    return r;
                }
                else if (operation=="newPorting")
                {
                    byte[] r = RequestNewPorting((byte[])args[0], (byte[])args[1], (BigInteger)args[2]);
                    return r.Length>0;
                }
                else if (operation=="ackDepositByUser")
                {
                    bool r = AckDepositByUser((byte[])args[0]);
                    return r;
                }
                else if (operation=="challengedeposit")
                {
                    bool r = ChallengeDeposit((byte[])args[0]);
                    return r;
                }
                else if (operation=="challengewithdraw")
                {
                    bool r = ChallengeWithdraw((byte[])args[0]);
                    return r;
                }
                else if (operation=="requestwithdraw")
                {
                    bool r = RequestWithdraw((byte[])args[0]);
                    return r;
                }
                else if (operation=="unlockcollateral")
                {
                    bool r = UnlockCollateral((byte[])args[0]);
                    return r;
                }
                else if (operation=="updatepriceoracle")
                {
                    if (!Runtime.CheckWitness(PriceOracle)) return false; // Only updatable by oracle
                    BigInteger price = (BigInteger)args[0];
                    Storage.Put("price", price);
                    return true;
                }
                else if (operation == "balanceOf") return BalanceOf((byte[])args[0]);
                else if (operation == "decimals") return Decimals();
                else if (operation == "name") return Name();
                else if (operation == "symbol") return Symbol();
                else if (operation == "supportedStandards") return SupportedStandards();
                else if (operation == "totalSupply") return TotalSupply();
                else if (operation == "transfer") return Transfer((byte[])args[0], (byte[])args[1], (BigInteger)args[2], ExecutionEngine.CallingScriptHash);
                else
                {
                    return false;
                }
            } else {
                // Someone is trying to spend NEO or GAS that have been sent to the contract
                // This should never happen, must have been user error 
                return true; // Allow anyone to spend it
            }
        }

        private static Balance deserializeBalance(byte[] balance){
            if (balance.Length != 0){
                return new Balance() { amount = 0, lastTimeTransfered = 0 };
            } else {
                return (Balance)Helper.Deserialize(balance);
            }
        }

        // DO NOT USE TO UPDATE COLLATERAL'S BALANCES, this function is an approximation of the real function and, as such, will always return a value lower than what the real function would. Although the difference is really small, this could be used by collaterals to reduce their collateral faster than permitted.
        private static BigInteger updateAmount(BigInteger amount, BigInteger lastTransfer, BigInteger currentTime){
            BigInteger deltaTime = currentTime - lastTransfer;

			// Code generated with NEP5interest.py, it essentially calculates the interest as in percentage^deltaTime in logarithmic time by using pre-computed constants
			byte[] rateDenominatorBytes = {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
			BigInteger rateDenominator = rateDenominatorBytes.AsBigInteger();
			byte[] magnitudeBytes = {0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
			BigInteger magnitude = magnitudeBytes.AsBigInteger();
            byte[][] rateNumeratorBytes = new byte[][] {
                new byte[]{0xa0, 0x9b, 0x94, 0x83, 0x2d, 0xbc, 0x75, 0x3c, 0x44, 0xd3, 0xb6, 0xd1, 0x26, 0x7c, 0x40, 0xb0, 0x3f, 0xe0, 0x44, 0xdb, 0x8a, 0xb3, 0x52, 0x90, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00},
                new byte[]{0x1a, 0x59, 0x5e, 0x72, 0xd7, 0xe8, 0xdf, 0xbc, 0xf0, 0xaa, 0xba, 0x6e, 0x7c, 0x0a, 0x4b, 0xaf, 0x06, 0x91, 0xb0, 0x52, 0x79, 0x41, 0x23, 0xc8, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00},
                new byte[]{0x5c, 0xff, 0x37, 0xaa, 0x6b, 0x1e, 0xcb, 0xc0, 0xf4, 0x1e, 0x3c, 0x12, 0xd2, 0xf3, 0x84, 0xf0, 0xdf, 0x74, 0x94, 0x0d, 0x7f, 0x1a, 0x10, 0xe4, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00},
                new byte[]{0x50, 0x62, 0xbb, 0x40, 0x66, 0xb1, 0x00, 0xfb, 0x8f, 0xb5, 0xe7, 0x34, 0x16, 0x58, 0xd2, 0x00, 0xfe, 0x31, 0xb6, 0xcc, 0xaa, 0xab, 0x07, 0xf2, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00},
                new byte[]{0x44, 0xea, 0x2c, 0xca, 0x2c, 0xad, 0xed, 0xe9, 0xc7, 0x37, 0x3c, 0x76, 0x8c, 0x7a, 0xb7, 0x07, 0x46, 0x91, 0x69, 0x8d, 0x6f, 0xbd, 0x03, 0xf9, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00},
                new byte[]{0x39, 0x53, 0xdb, 0x08, 0x3d, 0x63, 0xb7, 0x80, 0xba, 0x98, 0x6b, 0x97, 0x37, 0x68, 0x86, 0x2c, 0xc9, 0x53, 0x2a, 0x3b, 0x9e, 0xd8, 0x81, 0xfc, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00},
                new byte[]{0x2d, 0x9a, 0x32, 0x75, 0xee, 0x14, 0xee, 0x94, 0x3d, 0x69, 0x7e, 0x2a, 0x86, 0x7c, 0x07, 0x02, 0x2d, 0xc2, 0x08, 0xb8, 0xc8, 0xea, 0x40, 0xfe, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00},
                new byte[]{0xc0, 0x21, 0xfa, 0x09, 0x53, 0xf1, 0xd7, 0x73, 0xba, 0x8e, 0x3d, 0x75, 0xac, 0xc6, 0x1e, 0x3c, 0x56, 0x0d, 0x4c, 0xc2, 0x02, 0x75, 0x20, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00},
                new byte[]{0x2a, 0xb1, 0x14, 0x4a, 0x0b, 0xb9, 0x00, 0x25, 0x4e, 0x1d, 0x91, 0x55, 0x03, 0x27, 0x10, 0x9d, 0x78, 0x4a, 0xad, 0xfa, 0x68, 0x3a, 0x90, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00},
                new byte[]{0x79, 0x53, 0xff, 0x83, 0xf4, 0x9d, 0x76, 0x27, 0x85, 0xa4, 0x19, 0x73, 0x80, 0x7b, 0x12, 0xde, 0x46, 0x21, 0xb7, 0x63, 0x2e, 0x1d, 0xc8, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00},
                new byte[]{0xe6, 0xdf, 0x4f, 0xbe, 0x0b, 0xf4, 0x94, 0x37, 0x52, 0xf5, 0xba, 0x11, 0x66, 0x4b, 0xc1, 0xf0, 0x08, 0x85, 0x73, 0xab, 0x95, 0x0e, 0xe4, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00},
                new byte[]{0xfd, 0x87, 0x91, 0xa2, 0xa9, 0x79, 0x73, 0x6d, 0x07, 0x50, 0x1f, 0x2c, 0x48, 0x1a, 0x07, 0x34, 0x4a, 0xba, 0x1f, 0x74, 0x4a, 0x07, 0xf2, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00},
                new byte[]{0x4a, 0x51, 0x22, 0xa9, 0xf1, 0xd6, 0xdc, 0x57, 0xd0, 0xa4, 0x76, 0x98, 0x99, 0x14, 0x4c, 0x14, 0x6c, 0x5a, 0xa9, 0x21, 0xa5, 0x03, 0xf9, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00},
                new byte[]{0xa6, 0xb6, 0xbb, 0x6b, 0x3e, 0xc7, 0x57, 0x77, 0x6e, 0x22, 0xf2, 0xda, 0x36, 0x87, 0x23, 0x7a, 0x72, 0x0c, 0xbb, 0x8a, 0xd2, 0x81, 0xfc, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00},
                new byte[]{0x1b, 0x3c, 0xae, 0x8a, 0x76, 0x5a, 0x5c, 0x2d, 0x70, 0x65, 0x2e, 0xb1, 0x13, 0x86, 0x3e, 0xaf, 0x05, 0x1e, 0xd7, 0x43, 0xe9, 0x40, 0xfe, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00},
                new byte[]{0xbf, 0x93, 0xd4, 0xb8, 0x2b, 0xaf, 0x2b, 0xfe, 0x7f, 0x66, 0x28, 0x64, 0x8d, 0x1f, 0xf0, 0x9e, 0xf5, 0xf4, 0x89, 0xa1, 0x74, 0x20, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00},
                new byte[]{0x09, 0xe8, 0xac, 0x10, 0x67, 0x3d, 0xaf, 0xe1, 0x0a, 0x20, 0x77, 0x20, 0x5c, 0xfc, 0xa4, 0x76, 0xf7, 0x93, 0xac, 0x50, 0x3a, 0x90, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00},
                new byte[]{0xda, 0x42, 0x53, 0xdd, 0xa0, 0x79, 0x05, 0x16, 0x1e, 0xc1, 0x06, 0x7b, 0x01, 0xd0, 0xc8, 0xe3, 0x5a, 0x30, 0x50, 0x28, 0x1d, 0xc8, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00},
                new byte[]{0xe9, 0x72, 0x61, 0x04, 0x61, 0xfc, 0x77, 0xf4, 0xad, 0xa5, 0xf8, 0x51, 0x4b, 0x5f, 0xd7, 0x3b, 0xc5, 0x91, 0x26, 0x94, 0x0e, 0xe4, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00},
                new byte[]{0xbe, 0x5c, 0x22, 0x78, 0x58, 0x23, 0x75, 0x77, 0xdb, 0xb5, 0xcf, 0x04, 0xd3, 0x19, 0x63, 0x90, 0x48, 0xe7, 0x12, 0x4a, 0x07, 0xf2, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00},
                new byte[]{0x4c, 0x0c, 0x4c, 0x24, 0xd6, 0x27, 0x67, 0x8d, 0x10, 0x52, 0x2b, 0x30, 0x00, 0xbd, 0xce, 0xc4, 0x3d, 0x5b, 0x09, 0xa5, 0x03, 0xf9, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00}
            };

			while(deltaTime >= magnitude){
				BigInteger rateNumerator = rateNumeratorBytes[0].AsBigInteger();
				amount = (amount * rateNumerator) / rateDenominator;
				deltaTime -= magnitude;
			}
            for(int i = 1; i < rateNumeratorBytes.Length; i += 1){
                magnitude /= 2;
                if(deltaTime >= magnitude){
                    BigInteger rateNumerator = rateNumeratorBytes[1].AsBigInteger();
                    amount = (amount * rateNumerator) / rateDenominator;
                    deltaTime -= magnitude;
                }
            }

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
        public static string Name() => "smartBNB"; //name of the token

        [DisplayName("symbol")]
        public static string Symbol() => "SBNB"; //symbol of the token

        [DisplayName("supportedStandards")]
        public static string[] SupportedStandards() => new string[] { "NEP-5", "NEP-7", "NEP-10" };

        // FIXME: This is not getting updated
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
                fromBalance.amount = fromBalance.amount - amount;
                asset.Put(from, Helper.Serialize(fromBalance));
            }

            //Increase the payee balance
            Balance toBalance = updateBalance(deserializeBalance(asset.Get(to)));
            toBalance.amount = toBalance.amount + amount;
            asset.Put(to, Helper.Serialize(toBalance));

            Transferred(from, to, amount);
            return true;
        }

        public static object getCollatById(byte[] collatID)
        {
            byte[] collat = Storage.Get(collatID);
            if (collat.Length == 0) return null;
            return BytesToObject(collat);
        }

        public static bool putCollatById(byte[] collatID, Collat collat)
        {
            Storage.Put(collatID, ObjectToBytes(collat));
            return true;
        }

        public static object getPortingContract(byte[] portingContractID)
        {
            byte[] pc = Storage.Get(portingContractID);
            if (pc.Length == 0) return null;
            return BytesToObject(pc);
        }

        public static bool putPortingContract(byte[] portingContractID, PortingContract portingContract)
        {
            Storage.Put(portingContractID, ObjectToBytes(portingContract));
            return true;
        }

        public static bool executeChallenge(byte[] portingContractID, byte challengeNum, int sigNum, byte[][] pubks, byte[][] pubksDecompressed, params object[] args)
        {
            PortingContract pc = new PortingContract();
            Object p = getPortingContract(portingContractID);
            if(p==null) return false;
            pc = (PortingContract)p;

            BigInteger t = Runtime.Time-pc.LastTimestamp;
            if(t < CONTRACT_TIMEOUT_UPLOADPROOF || t > CONTRACT_TIMEOUT_UPLOADPROOF + WINDOW_CHALLENGE) return false;

            if (pc.ContractStatus==CONTRACT_STATUS_CHALLENGEWITHDRAW)
                portingContractID = portingContractID.Concat(new byte[]{CONTRACT_STATUS_CHALLENGEWITHDRAW});
            else if (pc.ContractStatus==CONTRACT_STATUS_CHALLENGEDEPOSIT)
                portingContractID = portingContractID.Concat(new byte[]{CONTRACT_STATUS_CHALLENGEDEPOSIT});

            bool challengeResult = true;

            if(challengeNum==0x1)
            {
                /* ARGS
                   0 byte[] collatid
                   1 byte[] txid
                   2 int signature index*/
                challengeResult = ChallengeInitialChecks(portingContractID, sigNum, byteP.AsBigInteger());
            }
            else if(challengeNum==0x2)
            {
                /* ARGS
                   0 byte[] collatid
                   1 byte[] txid
                   2 int signature index*/
                challengeResult = ChallengeCheckBytesV2(portingContractID, sigNum, pubks[sigNum]);
            }
            else if(challengeNum==0x3)
            {
                /* ARGS
                   0 byte[] collatid
                   1 byte[] txid
                   2 int signature index*/
                challengeResult = ChallengeSha512(portingContractID, sigNum);
            }
            else if(challengeNum==0x4)
            {
                /* ARGS
                   0 byte[] collatid
                   1 byte[] txid
                   2 int signature index*/
                challengeResult = ChallengeSha512ModQ(portingContractID, sigNum);
            }
            else if(challengeNum==0x5)
            {
                /* ARGS
                   0 byte[] collatid
                   1 byte[] txid
                   2 int signature index*/
                challengeResult = ChallengePointEqual(portingContractID, sigNum, byteP.AsBigInteger(), byteD.AsBigInteger());
            }
            else if(challengeNum==0x6){
                /* ARGS
                   0 byte[] collatid
                   1 byte[] txid
                   2 int signature index
                   3 int step num
                   4 string mulid*/
                challengeResult = ChallengeEdDSA_PointMul_Setp(portingContractID, sigNum, (int)args[2], (string)args[3], pubksDecompressed, byteP.AsBigInteger(), byteD.AsBigInteger());
            }
            else if(challengeNum==0x7){
                /* ARGS
                   0 byte[] collatid
                   1 byte[] txid*/
                challengeResult = ChallengeTxProof(portingContractID);
            }
            else if (challengeNum==0x8){
                challengeResult = ChallengeTxData(portingContractID);
            }
            else if (challengeNum==0x9){
                challengeResult = proofIsSaved(portingContractID);
            }

            if (!challengeResult)
            {
                if (pc.ContractStatus==CONTRACT_STATUS_CHALLENGEWITHDRAW)
                {
                    //EL COLLAT HA PERDIDO EL CHALLENGE, ENVIAR AL USUARIO NEP5
                    pc.ContractStatus = CONTRACT_STATUS_CHALLENGEWITHDRAW_FAILED;
                    return false;
                }
                else if (pc.ContractStatus==CONTRACT_STATUS_CHALLENGEDEPOSIT)
                {
                    //EL USUARIO HA PERDIDO EL CHALLENGE, ENVIAR NEP5 AL COLLAT NEP5 (solo el reward, no el collateral)
                    pc.ContractStatus = CONTRACT_STATUS_CHALLENGEDEPOSIT_FAILED;
                    return false;
                }
            }

            return challengeResult;
        }

        private static void TransferCGAS(byte[] from, byte[] to, BigInteger amount)
        {
            // Transfer token
            var args = new object[] { from, to, amount };
            if (!(bool)CGAS("transfer", args)) throw new Exception("Failed to transfer NEP-5 tokens!");
        }

        // Register a new collat or increase/decrease the deposit of an existing one
        public static bool RegisterAsCollateral(byte[] address, byte[] BNCAddress, BigInteger newAmount, byte operation)
        {
            if (!Runtime.CheckWitness(address)) return false;
            if (BNCAddress.Length!=20) return false;
            if (newAmount<1) return false;

            byte[] collatID = address.Concat(BNCAddress);
            Collat collat = new Collat();
            Object c = getCollatById(collatID);

            if (c == null)
            {
                collat.Address = address;
                collat.BNCAddress = BNCAddress;
                collat.CollateralAmount = newAmount;
                putCollatById(collatID, collat);
                TransferCGAS(address, ExecutionEngine.ExecutingScriptHash, newAmount);
                Deposited(address, newAmount);
            }
            else
            {
                collat = (Collat)c;
                if(operation==OPERATION_ADD)
                {
                    collat.CollateralAmount = collat.CollateralAmount+newAmount;;
                    putCollatById(collatID, collat);
                    TransferCGAS(address, ExecutionEngine.ExecutingScriptHash, newAmount);
                    Deposited(address, newAmount);
                }
                else if(operation==OPERATION_SUB)
                {   
                    BigInteger currentPrice = Storage.get("price").AsBigInteger;
                    if(newAmount > calculateCollateralAmountLeft(collat.CollateralAmount, currentPrice)) return false;
                    collat.CollateralAmount = collat.CollateralAmount - newAmount;
                    putCollatById(collatID, collat);
                    TransferCGAS(ExecutionEngine.ExecutingScriptHash, address, newAmount);
                }
            }
            return true;
        }

        public static BigInteger getCollateralAmount(BigInteger amountBNB, BigInteger currentPrice)
        {
            return (amountBNB*currentPrice*FACTOR_COLLATERAL_NUMERATOR)/(FACTOR_COLLATERAL_DENOMINATOR*PRICE_DENOMINATOR);
        }

        public static BigInteger calculateCollateralAmountLeft(Collat collat, BigInteger currentPrice)
        {
            return collat.CollateralAmount - getCollateralAmount(collat.CustodiedBNB, currentPrice);
        }

        public static byte[] RequestNewPorting(byte[] collatID, byte[] userAddr, BigInteger AmountBNB)
        {
            if (!Runtime.CheckWitness(userAddr)) return false;
            Collat collat = new Collat();
            Object c = getCollatById(collatID);
            if(c==null) return new byte[0];
            collat = (Collat)c;

            BigInteger currentPrice = Storage.get("price").AsBigInteger;

            BigInteger collateralAmountNedeed = getCollateralAmount(AmountBNB, currentPrice) + DEPOSIT_CHALLENGE; // Get the amount needed in GAS
            Storage.Put("amountn", collateralAmountNedeed); //DEBUG
            if(calculateCollateralAmountLeft(collat, currentPrice) < collateralAmountNedeed) return new byte[0];

            collat.CollateralAmount = collat.CollateralAmount - collateralAmountNedeed;
            putCollatById(collatID, collat);

            BigInteger timestamp = Runtime.Time;
            byte[] portingContractID = collatID.Concat(userAddr).Concat(timestamp.AsByteArray());
            if(getPortingContract(portingContractID) != null) return new byte[0];

            PortingContract pc = new PortingContract();
            pc.ContractStatus = CONTRACT_STATUS_PORTREQUEST;
            pc.CollatAddr = collatID.Range(0, 20);
            pc.CollatBCNAddr = collat.BNCAddress;
            pc.UserAddr=userAddr;
            pc.AmountBNB = AmountBNB;
            pc.LastTimestamp = timestamp;
            putPortingContract(portingContractID, pc);
            TransferCGAS(userAddr, ExecutionEngine.ExecutingScriptHash, (collateralAmountNedeed/FACTOR_PORTREQUEST_DIVISOR));
            return portingContractID;
        }

        public static bool AckDepositByUser(byte[] portingContractID)
        {
            byte[] collatAddr = portingContractID.Range(0, 20);
            if (!Runtime.CheckWitness(collatAddr)) return false;

            PortingContract pc = new PortingContract();
            Object p = getPortingContract(portingContractID);
            if(p==null) return false;
            pc = (PortingContract)p;
            if(pc.ContractStatus!=CONTRACT_STATUS_PORTREQUEST) return false;
            if((Runtime.Time-pc.LastTimestamp) > CONTRACT_TIMEOUT_PORTREQUEST) return false;

            pc.ContractStatus = CONTRACT_STATUS_DEPOSITOK;
            pc.LastTimestamp = Runtime.Time;

            putPortingContract(portingContractID, pc);

            TransferCGAS(ExecutionEngine.ExecutingScriptHash, pc.UserAddr, DEPOSIT_CHALLENGE); // TODO: FIX
            return true;
        }

        public static bool ChallengeDeposit(byte[] portingContractID)
        {
            // witness(useraddr)
            if (!Runtime.CheckWitness(portingContractID.Range(40,20))) return false; // TODO: Enable fishermen to also create challenges

            PortingContract pc = new PortingContract();
            Object p = getPortingContract(portingContractID);
            if(p==null) return false;
            pc = (PortingContract)p;

            if(pc.ContractStatus!=CONTRACT_STATUS_PORTREQUEST) return false;
            if(t < CONTRACT_TIMEOUT_PORTREQUEST || t > (CONTRACT_TIMEOUT_PORTREQUEST + WINDOW_CHALLENGE)) return false;

            pc.ContractStatus = CONTRACT_STATUS_CHALLENGEDEPOSIT;
            pc.LastTimestamp = Runtime.Time;

            putPortingContract(portingContractID, pc);
            TransferCGAS(pc.UserAddr, ExecutionEngine.ExecutingScriptHash, DEPOSIT_CHALLENGE);

            return true;
        }

        public static bool ChallengeWithdraw(byte[] portingContractID)
        {
            PortingContract pc = new PortingContract();
            Object p = getPortingContract(portingContractID);
            if(p==null) return false;
            pc = (PortingContract)p;

            if(pc.ContractStatus!=CONTRACT_STATUS_WITHDRAWREQUESTED) return false;
            BigInteger t= Runtime.Time-pc.LastTimestamp;
            if(t < CONTRACT_TIMEOUT_WITHDRAWREQUEST || t > (CONTRACT_TIMEOUT_WITHDRAWREQUEST + WINDOW_CHALLENGE)) return false;

            if (!Runtime.CheckWitness(pc.UserAddr)) return false; // TODO: Enable fishermen to also create challenges

            pc.ContractStatus = CONTRACT_STATUS_CHALLENGEWITHDRAW;
            pc.LastTimestamp = Runtime.Time;
            putPortingContract(portingContractID, pc);
            TransferCGAS(pc.UserAddr, ExecutionEngine.ExecutingScriptHash, DEPOSIT_CHALLENGE);

            return true;
        }

        public static bool RequestWithdraw(byte[] portingContractID)
        {
            
            Storage.Put("debug", "1");
            byte[] userAddr = portingContractID.Range(40, 20);
            Storage.Put("debug", userAddr);
            if (Runtime.CheckWitness(userAddr))
            {
                Storage.Put("debug", "3");
                PortingContract pc = new PortingContract();
                Object p = getPortingContract(portingContractID);
                if(p==null) return false;
                pc = (PortingContract)p;
                Storage.Put("debug", "4");
                if(pc.ContractStatus!=CONTRACT_STATUS_DEPOSITOK) return false;
                Storage.Put("debug", "5");
                if(userAddr!=pc.UserAddr) return false;
                Storage.Put("debug", "6");
                pc.ContractStatus = CONTRACT_STATUS_WITHDRAWREQUESTED;
                pc.LastTimestamp = Runtime.Time;

                //QUEMAR SBNB NEP5
                
                putPortingContract(portingContractID, pc);

                return true;
            }
            return false;
        }

        public static bool UnlockCollateral(byte[] portingContractID)
        {
            PortingContract pc = new PortingContract();
            Object p = getPortingContract(portingContractID);
            if(p==null) return false;
            pc = (PortingContract)p;

            byte[] collatID = portingContractID.Range(0, 40);
            Collat collat = new Collat();
            Object c = getCollatById(collatID);
            if(c==null) return false;
            collat = (Collat)c;

            BigInteger t = Runtime.Time - pc.LastTimestamp;

            if (pc.ContractStatus == CONTRACT_STATUS_WITHDRAWREQUESTED)
            {
                if (t > CONTRACT_TIMEOUT_WITHDRAWREQUEST + WINDOW_CHALLENGE)
                {
                    collat.CollateralAmountLeft = collat.CollateralAmountLeft + getCollateralByAmount(pc.AmountBNB) + DEPOSIT_CHALLENGE;
                }
            }
            else if (pc.ContractStatus == CONTRACT_STATUS_PORTREQUEST)
            {
                if (t > CONTRACT_TIMEOUT_PORTREQUEST + WINDOW_CHALLENGE)
                {
                    collat.CollateralAmountLeft = collat.CollateralAmountLeft + getCollateralByAmount(pc.AmountBNB) + DEPOSIT_CHALLENGE;
                }
            }
            else if (pc.ContractStatus == CONTRACT_STATUS_CHALLENGEWITHDRAW || pc.ContractStatus == CONTRACT_STATUS_CHALLENGEDEPOSIT)
            {//time to upload the proof + 
                if (t > CONTRACT_TIMEOUT_UPLOADPROOF + WINDOW_CHALLENGE)
                {
                    collat.CollateralAmountLeft = collat.CollateralAmountLeft + getCollateralByAmount(pc.AmountBNB) + DEPOSIT_CHALLENGE;
                }
            }
            else if (pc.ContractStatus == CONTRACT_STATUS_CHALLENGEDEPOSIT_FAILED)
            {
                collat.CollateralAmountLeft = collat.CollateralAmountLeft + getCollateralByAmount(pc.AmountBNB) + DEPOSIT_CHALLENGE;
            }
            else
                return false;

            putCollatById(collatID, collat);
            
            return true;
        }

        private static bool proofIsSaved(byte[] portingContractID)
        {
            PortingContract pc = new PortingContract();
            Object p = getCollatById(portingContractID);
            if(p==null) return false;
            pc = (PortingContract)p;

            byte[] addrAllowed;
            if (pc.ContractStatus==CONTRACT_STATUS_CHALLENGEWITHDRAW)
            {
                addrAllowed=portingContractID.Range(0, 20);
                portingContractID = portingContractID.Concat(new byte[]{CONTRACT_STATUS_CHALLENGEWITHDRAW});
            }
            else if (pc.ContractStatus==CONTRACT_STATUS_CHALLENGEDEPOSIT)
            {
                addrAllowed=portingContractID.Range(40, 20);
                portingContractID = portingContractID.Concat(new byte[]{CONTRACT_STATUS_CHALLENGEDEPOSIT});
            }
            else
                return false;

            string[] ss = {"", "Ps_ha0", "Ps_ha1", "ss_ha0", "ss_ha1", "Qs_ha0", "Qs_ha1", "Ps_sb0", "Ps_sb1", "ss_sb0", "Qs_sb0", "ss_sb1", "Qs_sb1"};
            byte[][] ids = new byte[ss.Length][];
            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = portingContractID.Concat(ss[i].AsByteArray());
            }

            for (int i = 0; i < ids.Length; i++)
            {
                if (Storage.Get(ids[i]).Length==0)
                {
                    Storage.Put("isSaved", ids[i]);
                    return false;
                }
            }
            Storage.Put("isSaved", "true");
            return true;
        }

        private static bool SaveChallengeState(params object[] args)
        {
            byte[] portingContractID = (byte[])args[0];

            PortingContract pc = new PortingContract();
            Object p = getCollatById(portingContractID);
            if(p==null) return false;
            pc = (PortingContract)p;

            byte[] addrAllowed;
            
            if(Runtime.Time-pc.LastTimestamp > CONTRACT_TIMEOUT_UPLOADPROOF) return false;
            
            if (pc.ContractStatus==CONTRACT_STATUS_CHALLENGEDEPOSIT)
            {
                addrAllowed=portingContractID.Range(0, 20);
                portingContractID = portingContractID.Concat(new byte[]{CONTRACT_STATUS_CHALLENGEWITHDRAW});
            }
            else if (pc.ContractStatus==CONTRACT_STATUS_CHALLENGEWITHDRAW)
            {
                addrAllowed=portingContractID.Range(40, 20);
                portingContractID = portingContractID.Concat(new byte[]{CONTRACT_STATUS_CHALLENGEDEPOSIT});
            }
            else
                return false;

            if (Runtime.CheckWitness(addrAllowed))
            {
                if (args.Length==11)
                    return saveStateToStorage(STG_GENERAL, portingContractID, args);
                else if (args.Length == 4)
                    return saveStateToStorage(STG_POINTMUL, portingContractID, args);
            }
            return false;
        }

        private static bool Validate(byte[] rawProof, byte[] rawHeader)
        {
            // Verify relationship with the block. Compares if hDataHash and txProofRootHash are equal and merkle path is ok
            int accLen = 0;
            //getting hDataHash
            for (int i = 0; i < 8; i++)
            {
                accLen += rawHeader[accLen]+1;
            }
            return VerifyTx(rawProof, rawHeader.Range(accLen+2, rawHeader[accLen]-1));
        }

        //deprecated
        private static bool VerifyBlock(byte[] rawHeader, byte[] rawSignatures)
        {
            //Obtaining header slices
            byte[][] headerSlices = new byte[16][];
            int accLen = 0;
            for (int i = 0; i < 16; i++)
            {
                headerSlices[i] = rawHeader.Range(accLen+1, rawHeader[accLen]);
                accLen += rawHeader[accLen]+1;
            }

            //Hashing header
            byte[] headerHash = SimpleHashFromByteSlices(headerSlices);

            /*//Obtaining signatures
              byte[][] signatures = new byte[11][];
              for (int i = 0; i < 11; i++)
              {
              signatures[i] = rawSignatures.Range(i*32, 32);
              }*/
            /*
               if (!VerifySignatures(headerHash, signatures))
               throw new Exception("Cruck");
               */

            return true;
        }

        private static byte[] HashRawHeader(byte[] rawHeader)
        {
            if (rawHeader.Length<2) return null;

            //Obtaining header slices
            byte[][] headerSlices = new byte[16][];
            int accLen = 0;
            int i = 0;

            while (accLen < rawHeader.Length && i < headerSlices.Length)
            {
                headerSlices[i] = rawHeader.Range(accLen+1, rawHeader[accLen]);
                accLen += rawHeader[accLen]+1;
                i++;
            }

            //Hashing header
            return SimpleHashFromByteSlices(headerSlices);

        }

        private static bool VerifyTx(byte[] proof, byte[] merkleRootFromHeader)
        {
            byte[] txProofLeafHash = proof.Range(0, 32);
            int txProofIndex = proof.Range(32, 1)[0];
            int txProofTotal = proof.Range(33, 1)[0];

            int len = proof.Range(34, proof.Length - 34).Length / 32;
            byte[][] txProofAunts = new byte[len][];
            for (int i = 0; i < len; i++)
            {
                txProofAunts[i] = proof.Range(34 + (i * 32), 32);
            }

            if (txProofIndex < 0)
                return false;//throw new Exception("Proof index cannot be negative");
            if (txProofTotal <= 0)
                return false;//throw new Exception("Proof total must be positive");

            byte[] computedHash = ComputeHashFromAunts(txProofIndex, txProofTotal, txProofLeafHash, txProofAunts);

            if (computedHash == null)
                return false;

            return AreEqual(computedHash, merkleRootFromHeader);
        }

        private static byte[] ComputeHashFromAunts(int index, int total, byte[] leafHash, byte[][] innerHashes)
        {
            if (index >= total)
                return null;

            switch (total)
            {
                case 0:
                    return null;//throw new Exception("Cannot call computeHashFromAunts() with 0 total");
                case 1:
                    if (innerHashes.Length != 0)
                        return null;

                    return leafHash;
                default:
                    if (innerHashes.Length == 0)
                        return null;

                    int numLeft = GetSplitPoint(total);
                    if(numLeft<1)
                        return null;

                    if (index < numLeft)
                    {
                        byte[] leftHash = ComputeHashFromAunts(index, numLeft, leafHash, TakeArrays(innerHashes, 0, innerHashes.Length - 2));
                        if (leftHash == null)
                            return null;

                        return InnerHash(leftHash, innerHashes[innerHashes.Length - 1]);
                    }

                    byte[] rightHash = ComputeHashFromAunts(index - numLeft, total - numLeft, leafHash, TakeArrays(innerHashes, 0, innerHashes.Length - 2));
                    if (rightHash == null)
                        return null;

                    return InnerHash(innerHashes[innerHashes.Length - 1], rightHash);
            }
        }

        // returns the largest power of 2 less than length
        private static int GetSplitPoint(int length)
        {
            if (length < 1)
                return 0;//throw new Exception("Trying to split a tree with size < 1");

            return (length % 2 == 0) ? length / 2 : (length + 1) / 2;
        }

        // returns Sha256(0x01 || left || right)
        private static byte[] InnerHash(byte[] left, byte[] right)
        {
            return Sha256(innerPrefix.Concat(left.Concat(right)));
        }

        // returns Sha256(0x00 || leaf)
        private static byte[] LeafHash(byte[] leaf)
        {
            return Sha256(leafPrefix.Concat(leaf));
        }

        private static bool AreEqual(byte[] a1, byte[] b1)
        {
            if (a1.Length == b1.Length)
            {
                int i = 0;
                while (i < a1.Length && (a1[i] == b1[i]))
                {
                    i++;
                }
                if (i == a1.Length)
                {
                    return true;
                }
            }
            return false;
        }

        // returns the byte arrays located between indexes ini and fin (both included)
        private static byte[][] TakeArrays(byte[][] arr, int ini, int fin)
        {
            int len = (fin - ini)+1;
            byte[][] cutted = new byte[len][];
            for (int i = 0; i < len; i++)
            {
                cutted[i] = arr[ini+i];
            }
            return cutted;
        }

        private static byte[] SimpleHashFromByteSlices(byte[][] slices)
        {
            switch (slices.Length)
            {
                case 0:
                    return null;
                case 1:
                    return LeafHash(slices[0]);
                default:
                    int k = GetSplitPoint(slices.Length);
                    if (k<1)
                        return null;

                    byte[] left = SimpleHashFromByteSlices(TakeArrays(slices, 0, k-1));
                    byte[] right = SimpleHashFromByteSlices(TakeArrays(slices, k, slices.Length-1));
                    return InnerHash(left, right);
            }
        }

        private static BigInteger SHA256(byte[] message)
        {
            // Implemented by NEOVM, see bytecode 0xA8 on https://docs.neo.org/developerguide/en/articles/neo_vm.html
            // https://docs.neo.org/docs/en-us/reference/scapi/fw/dotnet.html
            return Sha256(message).AsBigInteger();
        }

        private static BigInteger sha512modq(ulong[] num)
        {
            if (num.Length!=8) return -1;

            byte[] byteQ = {0xed, 0xd3, 0xf5, 0x5c, 0x1a, 0x63, 0x12, 0x58, 0xd6, 0x9c, 0xf7, 0xa2, 0xde, 0xf9, 0xde, 0x14, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10};
            BigInteger q = byteQ.AsBigInteger();

            BigInteger res = 0;
            BigInteger powers = 1;
            for(int i=0; i<8; i++){
                for(int j=0; j<8; j++){
                    for(int k =0; k<8; k++){
                        if(((num[i]>>(56+k)) & 1) == 1){
                            res = modsum(res, powers, q);
                        }
                        powers = modsum(powers, powers, q);
                    }
                    num[i] = num[i] << 8;
                }
            }
            return res;
        }

        private static BigInteger[] EdDSA_PointAdd(BigInteger[] P, BigInteger[] Q, BigInteger p, BigInteger  d)
        {	
            BigInteger A = mulmod(modPositive(P[1]-P[0], p), modPositive(Q[1]-Q[0], p), p);
            BigInteger B = mulmod(modsum(P[1],P[0], p), modsum(Q[1],Q[0], p), p);
            BigInteger C = mulmod(mulmod(modsum(P[3], P[3], p) , Q[3], p), d, p);
            BigInteger D = mulmod(modsum(P[2],P[2], p) , Q[2], p);
            BigInteger E= modPositive(B-A, p);
            BigInteger F= modPositive(D-C, p);
            BigInteger G= modsum(D,C, p);
            BigInteger H = modsum(B,A, p);

            BigInteger EF = mulmod(E,F,p);
            BigInteger GH = mulmod(G,H,p);
            BigInteger FG = mulmod(F,G,p);
            BigInteger EH = mulmod(E,H,p);

            return new BigInteger[4] { EF, GH, FG, EH };
        }

        //deprecated
        private static BigInteger[] EdDSA_PointMul(BigInteger s, BigInteger[] P, BigInteger p, BigInteger d)
        {
            BigInteger[] Q = { 0, 1, 1, 0 };
            while(s>0)
            {
                if ((s%2)==1)
                    Q = EdDSA_PointAdd(Q, P, p, d);
                P = EdDSA_PointAdd(P, P, p, d);
                s = s / 2;
            }
            return Q;
        }

        private static PointMulStep EdDSA_PointMul_step(PointMulStep step, BigInteger p, BigInteger d)
        {
            if(step.s>0)
            {
                if ((step.s%2)==1){
                    step.Q = EdDSA_PointAdd(step.Q, step.P, p, d);
                }
                step.P = EdDSA_PointAdd(step.P, step.P, p, d);
                step.s = step.s / 2;
            }
            return step;
        }

        private static PointMulStep EdDSA_PointMul_ByRange(PointMulStep initialStep, int itNum, BigInteger p, BigInteger d)
        {
            PointMulStep stepRecord = initialStep;
            for (int i=0; i<itNum; i++)
            {
                stepRecord = EdDSA_PointMul_step(stepRecord, p, d);
            }
            return stepRecord;
        }

        // To compute (a * b) % mod  
        private static BigInteger mulmod(BigInteger a, BigInteger b, BigInteger p)
        {
            byte[] bytePower127 = {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
            BigInteger power127 = bytePower127.AsBigInteger();
            BigInteger power128 = power127 * 2;
            BigInteger lowA = a%power128;
            BigInteger lowB = b%power127;
            BigInteger highA = a/power128;
            BigInteger highB = b/power127;
            BigInteger low = (lowA*lowB)%p;
            BigInteger high = (highA*highB)%p;
            BigInteger medium1 = ((lowA/2)*highB)%p;
            BigInteger medium2 = (lowB*highA)%p;
            BigInteger medium = modsum(medium1, medium2, p);
            medium = modsum(medium, medium, p);
            if(lowA%2 == 1){
                medium = modsum(medium, highB, p);
            }
            low = modsum(low, ((medium%power128)*power127)%p, p);
            high = modsum(high, medium/power128, p);
            high = mulmod256(high, 19, p);
            return modsum(high, low, p);
        }

        private static BigInteger mulmod256(BigInteger a, BigInteger b, BigInteger mod)  
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

            if (k<0)
                k+=p;

            return k;
        }

        private static BigInteger modrest(BigInteger a, BigInteger b, BigInteger p)
        {
            BigInteger r = a-b;
            return r < 0 ? r + p : r;
        }

        private static BigInteger modPositive(BigInteger x, BigInteger m)
        {
            return x < 0 ? (x + m) : (x % m);
        }

        public static ulong sum0(ulong v)
        {
            return ROTR(v, 28) ^ ROTR(v, 34) ^ ROTR(v, 39);
        }

        public static ulong sum1(ulong v)
        {
            return ROTR(v, 14) ^ ROTR(v, 18) ^ ROTR(v, 41);
        }

        public static ulong sig0(ulong v)
        {
            return ROTR(v, 1) ^ ROTR(v, 8) ^ (v >> 7);
        }

        public static ulong sig1(ulong v)
        {
            return ROTR(v, 19) ^ ROTR(v, 61) ^ (v >> 6);
        }

        public static ulong Ch(ulong x, ulong y, ulong z)
        {
            return (x & y) ^ ((~x) & z);
        }

        public static ulong Maj(ulong x, ulong y, ulong z)
        {
            return (x & y) ^ (x & z) ^ (y & z);
        }

        public static ulong ROTR(ulong v, int count)//61
        {
            ulong temp = (v >> count);

            ulong a = (ulong)0x7FFFFFFFFFFFFFFF >> (63 - count);
            ulong d = v & a;
            ulong temp1 = (d << (64 - count));

            ulong res = temp | temp1;

            return res;
        }

        //implementation of FIPS PUB 180-4 https://nvlpubs.nist.gov/nistpubs/FIPS/NIST.FIPS.180-4.pdf
        private static ulong[] sha512(ulong[] pre)
        {
            /** arg example
              ulong[] pre = { 7017280570803617792, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 24 };
              "abc"
              */

            if (pre.Length==0 || pre.Length%8!=0) return null;

            //constants
            ulong[] K = new ulong[80];
            K[0] = 4794697086780616226;
            K[1] = 8158064640168781261;
            K[2] = 13096744586834688815;
            K[3] = 16840607885511220156;
            K[4] = 4131703408338449720;
            K[5] = 6480981068601479193;
            K[6] = 10538285296894168987;
            K[7] = 12329834152419229976;
            K[8] = 15566598209576043074;
            K[9] = 1334009975649890238;
            K[10] = 2608012711638119052;
            K[11] = 6128411473006802146;
            K[12] = 8268148722764581231;
            K[13] = 9286055187155687089;
            K[14] = 11230858885718282805;
            K[15] = 13951009754708518548;
            K[16] = 16472876342353939154;
            K[17] = 17275323862435702243;
            K[18] = 1135362057144423861;
            K[19] = 2597628984639134821;
            K[20] = 3308224258029322869;
            K[21] = 5365058923640841347;
            K[22] = 6679025012923562964;
            K[23] = 8573033837759648693;
            K[24] = 10970295158949994411;
            K[25] = 12119686244451234320;
            K[26] = 12683024718118986047;
            K[27] = 13788192230050041572;
            K[28] = 14330467153632333762;
            K[29] = 15395433587784984357;
            K[30] = 489312712824947311;
            K[31] = 1452737877330783856;
            K[32] = 2861767655752347644;
            K[33] = 3322285676063803686;
            K[34] = 5560940570517711597;
            K[35] = 5996557281743188959;
            K[36] = 7280758554555802590;
            K[37] = 8532644243296465576;
            K[38] = 9350256976987008742;
            K[39] = 10552545826968843579;
            K[40] = 11727347734174303076;
            K[41] = 12113106623233404929;
            K[42] = 14000437183269869457;
            K[43] = 14369950271660146224;
            K[44] = 15101387698204529176;
            K[45] = 15463397548674623760;
            K[46] = 17586052441742319658;
            K[47] = 1182934255886127544;
            K[48] = 1847814050463011016;
            K[49] = 2177327727835720531;
            K[50] = 2830643537854262169;
            K[51] = 3796741975233480872;
            K[52] = 4115178125766777443;
            K[53] = 5681478168544905931;
            K[54] = 6601373596472566643;
            K[55] = 7507060721942968483;
            K[56] = 8399075790359081724;
            K[57] = 8693463985226723168;
            K[58] = 9568029438360202098;
            K[59] = 10144078919501101548;
            K[60] = 10430055236837252648;
            K[61] = 11840083180663258601;
            K[62] = 13761210420658862357;
            K[63] = 14299343276471374635;
            K[64] = 14566680578165727644;
            K[65] = 15097957966210449927;
            K[66] = 16922976911328602910;
            K[67] = 17689382322260857208;
            K[68] = 500013540394364858;
            K[69] = 748580250866718886;
            K[70] = 1242879168328830382;
            K[71] = 1977374033974150939;
            K[72] = 2944078676154940804;
            K[73] = 3659926193048069267;
            K[74] = 4368137639120453308;
            K[75] = 4836135668995329356;
            K[76] = 5532061633213252278;
            K[77] = 6448918945643986474;
            K[78] = 6902733635092675308;
            K[79] = 7801388544844847127;

            byte[] byteV = {0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,};
            ulong v = (ulong)byteV.AsBigInteger();

            ulong[] W = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            ulong[] H = new ulong[8];
            H[0] = 7640891576956012808;
            H[1] = 13503953896175478587;
            H[2] = 4354685564936845355;
            H[3] = 11912009170470909681;
            H[4] = 5840696475078001361;
            H[5] = 11170449401992604703;
            H[6] = 2270897969802886507;
            H[7] = 6620516959819538809;

            ulong a = H[0];
            ulong b = H[1];
            ulong c = H[2];
            ulong d = H[3];
            ulong e = H[4];
            ulong f = H[5];
            ulong g = H[6];
            ulong h = H[7];
            ulong T1=0;
            ulong T2=0;

            for(int j =0; j<pre.Length/16;j++)
            {
                a = H[0];
                b = H[1];
                c = H[2];
                d = H[3];
                e = H[4];
                f = H[5];
                g = H[6];
                h = H[7];

                for (int i = 0; i < 16; i++)
                {
                    W[i] = pre[(j*16)+i] & v;
                }

                for (int i = 16; i < 80; i++)
                {
                    W[i] = (sig1(W[i - 2]) + W[i - 7] + sig0(W[i - 15]) + W[i - 16])&v;
                }

                for (int i = 0; i < 80; i++)
                {
                    T1 = (h + sum1(e) + Ch(e, f, g) + K[i] + W[i])&v;
                    T2 = (sum0(a) + Maj(a, b, c))&v;
                    h = g;
                    g = f;
                    f = e;
                    e = (d + T1)&v;
                    d = c;
                    c = b;
                    b = a;
                    a = (T1 + T2)&v;


                }

                H[0] = (a+H[0])&v;
                H[1] = (b+H[1])&v;
                H[2] = (c+H[2])&v;
                H[3] = (d+H[3])&v;
                H[4] = (e+H[4])&v;
                H[5] = (f+H[5])&v;
                H[6] = (g+H[6])&v;
                H[7] = (h+H[7])&v;

            }

            return H;
        }

        /**
          usage example
          int ini = 9;
          int fin = 88;

        //pre = 0aaaaaaaabc00...0024
        ulong[] pre = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 24 };
        pre[0]=27410143614427489;
        pre[1]=7017280570803617792;

        byte[] bytes = "aaaaaaaabc".AsByteArray();

        bool res = checkBytes(pre, bytes, ini, fin);
        */
        //deprecated
        public static bool checkBytes(ulong[] pre, byte[] bytes, int ini, int fin)
        {

            if(bytes.Length!=fin/8)
                return false;

            ulong val = pre[(fin - 1) / 64];

            byte byt = bytes[bytes.Length-1];

            ulong itmsg = ((ulong)1)<<(64 - (fin - 64));
            ulong itbyt = 1;

            for (int i=0; i<(fin - ini); i++)
            {
                if ((64-(fin%64)+i) % 64 == 0 & i != 0)
                {
                    val = pre[((fin - 1) / 64)-((64 - (fin % 64) + i) / 64)];
                    itmsg = 1;
                }

                if (i % 8 == 0)
                {
                    byt = bytes[((bytes.Length*8-i) / 8)-1];
                    itbyt = 1;
                }

                if(((byt & itbyt)>>(i%8)) == ((val & itmsg)>> ((64 - (fin - 64-i))%64)))
                {
                    itbyt = itbyt << 1;
                    itmsg = itmsg << 1;
                }
                else{
                    return false;
                }

            }
            return true;
        }

        private static bool point_equal(BigInteger[] P, BigInteger[] Q, BigInteger p)
        {
            if (modrest(mulmod(P[0], Q[2], p), mulmod(Q[0], P[2], p), p) != 0)
                return false;
            if (modrest(mulmod(P[1], Q[2], p), mulmod(Q[1], P[2], p), p) != 0)
                return false;
            return true;
        }

        //deprecated
        private static bool verify_signature(
                BigInteger A0_xPubK, BigInteger A1_yPubK, byte[] pubK,
                BigInteger R0_xSigHigh, BigInteger R1_ySigHigh, byte[] signature,
                ulong[] pre, byte[] signableBytes, byte[] blockHash)
        {
            byte[] byteP = {0xed, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x7f};
            BigInteger p = byteP.AsBigInteger();

            byte[] byteD = {0xa3, 0x78, 0x59, 0x13, 0xca, 0x4d, 0xeb, 0x75, 0xab, 0xd8, 0x41, 0x41, 0x4d, 0x0a, 0x70, 0x00, 0x98, 0xe8, 0x79, 0x77, 0x79, 0x40, 0xc7, 0x8c, 0x73, 0xfe, 0x6f, 0x2b, 0xee, 0x6c, 0x03, 0x52};
            BigInteger d = byteD.AsBigInteger();

            if (signature.Length!=64)
                return false;//throw new Exception("Bad signature length");

            byte[] Rs_signatureHigh = signature.Range(0, 32);

            if (!checkCompressed(R0_xSigHigh, R1_ySigHigh, Rs_signatureHigh, p))
                return false;//throw new Exception("Relationship between compressed and decompressed public point not found");

            byte[] q_bytes = {0xed, 0xd3, 0xf5, 0x5c, 0x1a, 0x63, 0x12, 0x58, 0xd6, 0x9c, 0xf7, 0xa2, 0xde, 0xf9, 0xde, 0x14, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10};
            BigInteger q = q_bytes.AsBigInteger();

            byte[] s_signatureLow = signature.Range(32, 32);
            BigInteger s = s_signatureLow.AsBigInteger();
            if (s>=q || s<0)
                return false;

            for (int i=0; i<32;i++)
            {
                if(blockHash[i]!=signableBytes[16+i])
                    return false;//throw new Exception("Hash not contained in signBytes");
            }

            byte[] hashableBytes = Rs_signatureHigh.Concat(pubK).Concat(signableBytes);

            if (!checkBytes(pre, hashableBytes, 1, (int)pre[pre.Length-1])){
                return false;//throw new Exception("Wrong padded message");
            }

            ulong[] hash = sha512(pre);
            BigInteger h = sha512modq(hash);

            byte[] g1bytes = {0x1a, 0xd5, 0x25, 0x8f, 0x60, 0x2d, 0x56, 0xc9, 0xb2, 0xa7, 0x25, 0x95, 0x60, 0xc7, 0x2c, 0x69, 0x5c, 0xdc, 0xd6, 0xfd, 0x31, 0xe2, 0xa4, 0xc0, 0xfe, 0x53, 0x6e, 0xcd, 0xd3, 0x36, 0x69, 0x21};
            BigInteger g1 = g1bytes.AsBigInteger();
            byte[] g2bytes = {0x58, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66};
            BigInteger g2 = g2bytes.AsBigInteger();
            byte[] g4bytes = {0xa3, 0xdd, 0xb7, 0xa5, 0xb3, 0x8a, 0xde, 0x6d, 0xf5, 0x52, 0x51, 0x77, 0x80, 0x9f, 0xf0, 0x20, 0x7d, 0xe3, 0xab, 0x64, 0x8e, 0x4e, 0xea, 0x66, 0x65, 0x76, 0x8b, 0xd7, 0x0f, 0x5f, 0x87, 0x67};
            BigInteger g4 = g4bytes.AsBigInteger();
            BigInteger[] G = {g1, g2,1, g4};

            BigInteger[] A = {A0_xPubK, A1_yPubK, 1, mulmod(A0_xPubK, A1_yPubK, p)};

            BigInteger[] sB = EdDSA_PointMul(s, G, p, d);

            BigInteger[] hA = EdDSA_PointMul(h, A, p, d);

            BigInteger[] R = {R0_xSigHigh, R1_ySigHigh, 1, mulmod(R0_xSigHigh, R1_ySigHigh, p)};

            bool isSigOK = point_equal(sB, EdDSA_PointAdd(R, hA, p, d), p);

            return isSigOK;
        }

        private static bool checkCompressed(BigInteger x, BigInteger y, byte[] compressed, BigInteger p)
        {
            if(x<0 || x>=p){
                return false;
            }
            if(y<0 || y>=p){
                return false;
            }
            //Check that point belongs in the curve
            BigInteger x2 = mulmod(x, x, p);
            BigInteger y2 = mulmod(y, y, p);
            //((-x**2+y**2-1)*121666)%p==(-121665*x**2*y**2)%p
            if(mulmod(((modrest(y2, x2, p)-1)%p), 121666, p) != mulmod(p-121665, mulmod(x2, y2, p), p)){
                return false; //Point doesn't belong in the curve
            }
            //int.to_bytes(y | ((x & 1) << 255), 32, "little")
            int sign = compressed[31] & 0x80;
            if((sign>>7) != (x%2)){
                return false; //Compressed is wrong
            }

            int index31 = 31;
            byte withSign = compressed[31];
            byte noSign = (byte)(compressed[31] & 0x7F);
            compressed[index31] = noSign;
            if(compressed.AsBigInteger() != y){
                return false;
            }

            compressed[index31] = withSign;

            return true;
        }


        private static object BytesToObject(byte[] bytes)
        {
            if (bytes.Length == 0)
            {
                return new object();
            }
            else
            {
                object[] objs = (object[])Helper.Deserialize(bytes);
                return (object)objs;
            }
        }

        private static byte[] ObjectToBytes(object obj)
        {
            return Helper.Serialize(obj);
        }

        private static Object getStateFromStorage(byte state, byte[] stg_key, params object[] args)
        {
            if (state == STG_POINTMUL)
            {
                byte[] pointMulID = ((string)args[0]).AsByteArray();
                stg_key = stg_key.Concat(pointMulID);
            }

            byte[] stg = Storage.Get(stg_key);
            if (stg.Length == 0) return null;
            return BytesToObject(stg);
        }

        private static bool saveStateToStorage(byte state, byte[] stg_key, params object[] args)
        {
            if(state == STG_GENERAL)
            {
                GeneralChallengeVariables challengeVars = new GeneralChallengeVariables();
                challengeVars.signature = (byte[][])args[1];
                if (challengeVars.signature.Length!=8) return false;

                challengeVars.xs = (BigInteger[])args[2];
                if (challengeVars.xs.Length!=8) return false;

                challengeVars.ys = (BigInteger[])args[3];
                if (challengeVars.ys.Length!=8) return false;

                challengeVars.signableBytes = (byte[][])args[4];
                if (challengeVars.signableBytes.Length!=8) return false;

                challengeVars.pre = (ulong[][])args[5];
                if (challengeVars.pre.Length!=8) return false;

                challengeVars.preHash = (ulong[][])args[6];
                if (challengeVars.preHash.Length!=8) return false;

                challengeVars.preHashMod = (BigInteger[])args[7];
                if (challengeVars.preHashMod.Length!=8) return false;

                challengeVars.txproof = (byte[])args[8];
                if (challengeVars.txproof.Length==0) return false;

                challengeVars.blockHeader = (byte[])args[9];
                if (challengeVars.blockHeader.Length==0) return false;

                challengeVars.txBytes = (ulong[])args[10];
                if (challengeVars.txBytes.Length<3) return false;

                Storage.Put(stg_key, ObjectToBytes(challengeVars));
                return true;
            }
            else if(state == STG_POINTMUL)
            {
                byte[] pointMulID = ((string)args[2]).AsByteArray();
                stg_key = stg_key.Concat(pointMulID);

                string type = (string)args[1];
                if(type=="simple")
                {
                    BigInteger[] data = (BigInteger[])args[3];
                    if (data.Length != slicesLen) return false;

                    Storage.Put(stg_key, ObjectToBytes(data));
                }
                else
                {
                    BigInteger[][] data = (BigInteger[][])args[3];
                    if (data.Length != slicesLen) return false;

                    for (int i=0; i< data.Length; i++)
                        if (data[i].Length!=4) return false;

                    Storage.Put(stg_key, ObjectToBytes(data));
                }

                return true;
            }
            return false;
        }

        private static bool ChallengeInitialChecks(byte[] portingContractID, int sigIndex, BigInteger p)
        {
            GeneralChallengeVariables challengeVars = new GeneralChallengeVariables();
            Object o = getStateFromStorage(STG_GENERAL, portingContractID, null);
            if (o==null) return false;
            challengeVars = (GeneralChallengeVariables)o;

            byte[] signature = challengeVars.signature[sigIndex];
            BigInteger R0_xSigHigh = challengeVars.xs[sigIndex];
            BigInteger R1_ySigHigh = challengeVars.ys[sigIndex];
            byte[] signableBytes = challengeVars.signableBytes[sigIndex];
            byte[] rawHeader = challengeVars.blockHeader;

            if (signature.Length!=64)
                return false;//throw new Exception("Bad signature length");

            byte[] Rs_signatureHigh = signature.Range(0, 32);

            if (!checkCompressed(R0_xSigHigh, R1_ySigHigh, Rs_signatureHigh, p))
                return false;//throw new Exception("Relationship between compressed and decompressed public point not found");

            byte[] q_bytes = {0xed, 0xd3, 0xf5, 0x5c, 0x1a, 0x63, 0x12, 0x58, 0xd6, 0x9c, 0xf7, 0xa2, 0xde, 0xf9, 0xde, 0x14, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10};
            BigInteger q = q_bytes.AsBigInteger();

            byte[] s_signatureLow = signature.Range(32, 32);
            BigInteger s = s_signatureLow.AsBigInteger();
            if (s>=q || s<0)
                return false;

            byte[] blockHash = HashRawHeader(rawHeader);

            byte round = signableBytes.Range(0, 1)[0];

            int blockHashStart = round > 0 ? 27 : 17;

            return AreEqual(blockHash, signableBytes.Range(blockHashStart, 32));
            //if return false; -> throw new Exception("Hash not contained in signableBytes");
        }

        private static bool ChallengeCheckBytesV2(byte[] portingContractID, int sigIndex, byte[] pubK)
        {
            GeneralChallengeVariables challengeVars = new GeneralChallengeVariables();
            Object o = getStateFromStorage(STG_GENERAL, portingContractID, null);
            if (o==null) return false;
            challengeVars = (GeneralChallengeVariables)o;

            ulong[] pre = challengeVars.pre[sigIndex];
            if (pre.Length==0 || pre.Length%8!=0) return false;

            byte[] signature = challengeVars.signature[sigIndex];
            byte[] signableBytes = challengeVars.signableBytes[sigIndex];
            signableBytes = signableBytes.Range(1, signableBytes.Length-1);
            byte[] Rs_signatureHigh = signature.Range(0, 32);
            byte[] hashableBytes = Rs_signatureHigh.Concat(pubK).Concat(signableBytes);

            byte[] preBytes = ObjectToBytes(pre);
            return CheckBytesv2(preBytes, hashableBytes);
        }

        //deprecated
        private static bool ChallengeCheckBytes(byte[] portingContractID, int sigIndex, byte[] pubK)
        {
            GeneralChallengeVariables challengeVars = new GeneralChallengeVariables();
            Object o = getStateFromStorage(STG_GENERAL, portingContractID, null);
            if (o==null) return false;
            challengeVars = (GeneralChallengeVariables)o;

            ulong[] pre = challengeVars.pre[sigIndex];
            byte[] signature = challengeVars.signature[sigIndex];
            byte[] signableBytes = challengeVars.signableBytes[sigIndex];
            byte[] Rs_signatureHigh = signature.Range(0, 32);
            byte[] hashableBytes = Rs_signatureHigh.Concat(pubK).Concat(signableBytes.Range(1, signableBytes.Length-1));

            return checkBytes(pre, hashableBytes, 1, (int)pre[pre.Length-1]);
        }

        private static bool ChallengeSha512(byte[] portingContractID, int sigIndex)
        {
            GeneralChallengeVariables challengeVars = new GeneralChallengeVariables();
            Object o = getStateFromStorage(STG_GENERAL, portingContractID, null);
            if (o==null) return false;
            challengeVars = (GeneralChallengeVariables)o;

            ulong[] pre = challengeVars.pre[sigIndex];
            ulong[] hash = challengeVars.preHash[sigIndex];

            ulong[] expectedHash = sha512(pre);

            if (expectedHash != null && expectedHash.Length == hash.Length)
            {
                int i = 0;
                while (i < expectedHash.Length && (expectedHash[i] == hash[i]))
                    i++;
                if (i == expectedHash.Length)
                    return true;
            }

            return false;
        }

        private static bool ChallengeSha512ModQ(byte[] portingContractID, int sigIndex)
        {
            GeneralChallengeVariables challengeVars = new GeneralChallengeVariables();
            Object o = getStateFromStorage(STG_GENERAL, portingContractID, null);
            if (o==null) return false;
            challengeVars = (GeneralChallengeVariables)o;

            ulong[] hash = challengeVars.preHash[sigIndex];
            BigInteger mod = challengeVars.preHashMod[sigIndex];

            BigInteger expectedMod = sha512modq(hash);

            return (expectedMod==mod);
        }

        private static bool ChallengePointEqual(byte[] portingContractID, int sigIndex, BigInteger p, BigInteger d)
        {
            int iint = 31;
            int istep = (sigIndex*32+iint)%slicesLen;
            int iarr = (iint+sigIndex*32)/slicesLen; 
            string bs_sb = "sb"+((BigInteger)(iarr+48)).AsByteArray().AsString();
            string bs_ha = "ha"+((BigInteger)(iarr+48)).AsByteArray().AsString();
            string Qbs = "Qs_"+bs_sb;
            BigInteger[][] Qssb = (BigInteger[][])getStateFromStorage(STG_POINTMUL, portingContractID, Qbs);

            /*Object o = getStateFromStorage(STG_POINTMUL, collatId, txHash, Qbs);
              if (o==null) return false;
              BigInteger[][] Qssb = (BigInteger[][])o;*/

            Qbs = "Qs_"+bs_ha;
            BigInteger[][] Qsha = (BigInteger[][])getStateFromStorage(STG_POINTMUL, portingContractID, Qbs);
            BigInteger[] sB = Qssb[istep];
            BigInteger[] hA = Qsha[istep];

            GeneralChallengeVariables challengeVars = new GeneralChallengeVariables();
            Object o = getStateFromStorage(STG_GENERAL, portingContractID, null);
            if (o==null) return false;
            challengeVars = (GeneralChallengeVariables)o;

            BigInteger R0_xSigHigh = challengeVars.xs[sigIndex];
            BigInteger R1_ySigHigh = challengeVars.ys[sigIndex];

            BigInteger[] R = {R0_xSigHigh, R1_ySigHigh, 1, mulmod(R0_xSigHigh, R1_ySigHigh, p)};

            return point_equal(sB, EdDSA_PointAdd(R, hA, p, d), p);
        }

        private static bool ChallengeEdDSA_PointMul_Setp(byte[] portingContractID, int sigIndex, int i, string mulid, byte[][] pubksDecompressed, BigInteger p, BigInteger d)
        {
            int istep = (sigIndex*32+i-1)%slicesLen;//array index, negativeValue%value does not work in vm so its ok here
            int iarr = (i+sigIndex*32)/slicesLen;
            string bs = mulid+((BigInteger)(iarr+48)).AsByteArray().AsString();

            string Pbs = "Ps_"+bs;
            BigInteger[][] Ps = (BigInteger[][])getStateFromStorage(STG_POINTMUL, portingContractID, Pbs);
            string Qbs = "Qs_"+bs;
            BigInteger[][] Qs = (BigInteger[][])getStateFromStorage(STG_POINTMUL, portingContractID, Qbs);
            string sbs = "ss_"+bs;
            BigInteger[] ss = (BigInteger[])getStateFromStorage(STG_POINTMUL, portingContractID, sbs);

            PointMulStep initialStep = new PointMulStep();
            PointMulStep expectedStep = new PointMulStep();

            if (modPositive(istep,32)-32==-1)
            {
                GeneralChallengeVariables challengeVars = new GeneralChallengeVariables();
                Object o = getStateFromStorage(STG_GENERAL, portingContractID, null);
                if (o==null) return false;
                challengeVars = (GeneralChallengeVariables)o;

                initialStep.Q = new BigInteger[]{ 0, 1, 1, 0 };

                if(mulid=="sb")
                {
                    BigInteger[] G = new BigInteger[4];
                    byte[] g0 = {0x1a, 0xd5, 0x25, 0x8f, 0x60, 0x2d, 0x56, 0xc9, 0xb2, 0xa7, 0x25, 0x95, 0x60, 0xc7, 0x2c, 0x69, 0x5c, 0xdc, 0xd6, 0xfd, 0x31, 0xe2, 0xa4, 0xc0, 0xfe, 0x53, 0x6e, 0xcd, 0xd3, 0x36, 0x69, 0x21};
                    G[0] = g0.AsBigInteger();
                    byte[] g1 = {0x58, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66};
                    G[1] =g1.AsBigInteger();
                    G[2] = 1;
                    byte[] g3 = {0xa3, 0xdd, 0xb7, 0xa5, 0xb3, 0x8a, 0xde, 0x6d, 0xf5, 0x52, 0x51, 0x77, 0x80, 0x9f, 0xf0, 0x20, 0x7d, 0xe3, 0xab, 0x64, 0x8e, 0x4e, 0xea, 0x66, 0x65, 0x76, 0x8b, 0xd7, 0x0f, 0x5f, 0x87, 0x67};
                    G[3] =g3.AsBigInteger();
                    initialStep.P = G;

                    byte[] signature = challengeVars.signature[sigIndex];
                    byte[] s_signatureLow = signature.Range(32, 32);
                    BigInteger s = s_signatureLow.AsBigInteger();

                    initialStep.s = s;
                }
                else if(mulid=="ha")
                {
                    BigInteger[] A = new BigInteger[4];
                    A[0] = pubksDecompressed[sigIndex*3].AsBigInteger();
                    A[1] = pubksDecompressed[sigIndex*3+1].AsBigInteger();
                    A[2] = 1;
                    A[3] = pubksDecompressed[sigIndex*3+2].AsBigInteger();

                    initialStep.P = A;

                    BigInteger[] mods = challengeVars.preHashMod;
                    initialStep.s = mods[sigIndex];
                }
            }
            else
            {
                initialStep.s = ss[istep];
                initialStep.P = Ps[istep];
                initialStep.Q = Qs[istep];
            }

            expectedStep.Q = Qs[istep+1];
            expectedStep.s = ss[istep+1];
            expectedStep.P = Ps[istep+1];

            PointMulStep res = new PointMulStep();
            res = EdDSA_PointMul_ByRange(initialStep, 8, p, d);

            if ((expectedStep.Q[0] == res.Q[0])&
                    (expectedStep.Q[1] == res.Q[1])&
                    (expectedStep.Q[2] == res.Q[2])&
                    (expectedStep.Q[3] == res.Q[3])&
                    (expectedStep.s ==res.s)&
                    (expectedStep.P[0] == res.P[0])&
                    (expectedStep.P[1] == res.P[1])&
                    (expectedStep.P[2] == res.P[2])&
                    (expectedStep.P[3] == res.P[3]))
                return true;
            else
                return false;
        }

        private static bool ChallengeTxProof(byte[] portingContractID)
        {
            GeneralChallengeVariables challengeVars = new GeneralChallengeVariables();
            Object o = getStateFromStorage(STG_GENERAL, portingContractID, null);
            if (o==null) return false;
            challengeVars = (GeneralChallengeVariables)o;

            byte[] txproof = challengeVars.txproof;
            byte[] blockHeader = challengeVars.blockHeader;
            return Validate(txproof, blockHeader);
        }

        public static BigInteger String2BigInteger(string str)
        {
            BigInteger res = 0;
            byte[] a = str.AsByteArray();
            int mult = 0;
            byte[] k = new byte[1];
            int index = 0;
            for (int i=0; i<a.Length; i++)
            {
                byte b = a[i];
                k[index] = b;

                if(a.Length-i==3)
                    mult=100;
                else if(a.Length-i==2)
                    mult=10;
                else if(a.Length-i==1)
                    mult=1;
                else
                    mult=0;

                res+=((k.AsBigInteger()-48)*mult);
            }
            return res;
        }

        private static bool CheckBytesv2(byte[] preBytes, byte[] bytes)
        { //TODO: test!!!!
            bool end = false;

            int ipreB = 0;
            int ib = 0;
            int len = 1;

            while(ipreB < preBytes.Length)
            {
                ipreB += len+2;
                len = preBytes[ipreB];

                if (len==0)
                    continue;

                if(!end){
                    for (int j = 0; j<(8-len); j++)
                    {
                        if (bytes[ib] != 0)
                        {
                            return false;
                        }
                        ib++;
                    }
                    for (int i=len; i>0; i--)
                    {
                        if(i > 8)
                            if(preBytes[ipreB+i] == 0)
                                continue;
                            else
                                return false;

                        if(ib == bytes.Length && preBytes[ipreB+i] == 0x80)
                            end = true;
                        else if (!end && preBytes[ipreB+i] != bytes[ib])
                            return false;
                        else if (end && preBytes[ipreB+i] != 0)
                            return false;
                        else
                            ib++;
                    }
                }
                else
                {
                    BigInteger ulen = bytes.Length*8;
                    byte[] b_ulen = ulen.AsByteArray();
                    for (int i=0; i<len; i++)
                    {
                        if (preBytes[ipreB+i+1]!=b_ulen[i])
                            return false;
                    }
                    return true;
                }
            }
            return false;
        }

        //get leafHash from encoded tx        
        private static byte[] getLeafHashByTxBytes(byte[] tx)
        {
            return LeafHash(Sha256(tx));
        }

        //unmarshal

        [Serializable]
        struct Output
        {
            public byte[] addr;
            public BigInteger amount;
            public string denom;
        }

        private static ulong[] Uvarint(ulong [] bz, int[] ini_fin)
        {
            ulong x=0;
            int s=0;
            ulong b=0;

            for (int i = 0; i<(ini_fin[1]-ini_fin[0]+1); i++)
            {
                b = bz[ini_fin[0]+i];
                if (b < 128)
                {
                    if (i>9 || i==9 && b>1)
                        return new ulong[]{0, 0-((ulong)i+1)};

                    return new ulong[]{x|(b<<s), (ulong)i+1};
                }
                ulong n = (b&0x7f) << s;
                x |= n;
                s += 7;
            }
            return new ulong[]{0, 0};
        }

	//TODO: test unexpected faults
        private static ulong[] DecodeUvarint(ulong[] bz, int[] ini_fin)
        {
            ulong[] un = Uvarint(bz, ini_fin);

            if (un[1]<=0)
                return new ulong[]{0, 0};

            return un;
        }

        private static int[] DecodeByteSlice(ulong[] bz, int[] ini_fin)
        {
            ulong[] count_n = DecodeUvarint(bz, ini_fin);

            if (count_n[0]<0 || (ini_fin[1]-ini_fin[0])+1 < (int)count_n[0])
                return new int[]{0, 0};

            ini_fin[0] = ini_fin[0] + (int)count_n[1];
            ini_fin[1] = ini_fin[0] + (int)count_n[0] - (int)count_n[1];
            return ini_fin;
        }

        private static Output decodeOutput(ulong[] bz, int ini, int len)
        {
            Storage.Put("ini", ini);
            Storage.Put("len", len);
            Output o = new Output();
            int[] ini_fin = new int[2];
            ini_fin[0] = ini;
            ini_fin[1] = ini+len-1;

            //decode struct
            ini_fin = DecodeByteSlice(bz, ini_fin);
            if (ini_fin[0] == ini_fin[1])
                return o;

            //skip decoding of field number and type
            ini_fin[0]=ini_fin[0]+1;

            //DECODE ADDRESS
            int[] add_ini_fin = DecodeByteSlice(bz, ini_fin);
            int addLen = (add_ini_fin[1]-add_ini_fin[0])+1;
            if (addLen != 20)
                return o;

            byte[] address = new byte[20];
            byte b;

            for (int i = 0; i < 20; i++)
            {
                b=(byte)bz[add_ini_fin[0]+i];
                address[i] = b;
            }
            o.addr = address;
            //slide till coins
            ini_fin[0] = add_ini_fin[1]+1;
            ini_fin[1] = bz.Length-1;

            //skip decoding of field number and type
            ini_fin[0]=ini_fin[0]+3;

            //DECODE ASSET
            int[] ass_ini_fin = DecodeByteSlice(bz, ini_fin);
            byte[] asset = new byte[3];
            for (int i = 0; i < 3; i++)
            {
                b=(byte)bz[ass_ini_fin[0]+i];
                asset[i] = b;
            }
            o.denom = asset.AsString();

            //slide till amount
            ini_fin[0] = ass_ini_fin[1]+1;
            ini_fin[1] = bz.Length-1;

            //skip decoding of field number and type
            ini_fin[0]=ini_fin[0]+1;

            ulong[] num_n = DecodeUvarint(bz, ini_fin);
            BigInteger amount = num_n[0];
            o.amount = amount;

            return o;
        }

        //TODO control unexpected faults
        private static bool ChallengeTxData(byte[] portingContractID)
        {
            GeneralChallengeVariables challengeVars = new GeneralChallengeVariables();
            Object o = getStateFromStorage(STG_GENERAL, portingContractID, null);
            if (o==null) return false;

            challengeVars = (GeneralChallengeVariables)o;
            ulong[] txb = challengeVars.txBytes;

            int start = (int)txb[0];
            int len = (int)txb[1];

            if (start<0 || len < txb.Length-2) return false;

            Output output = new Output();
            output = decodeOutput(txb, start, start+len);

            byte[] bytestx = new byte[350];//TODO: DINAMIC LEN
            byte bt;
            for (int i = 0; i<txb.Length-2;i++)
            {
                bt = (byte)txb[i+2];
                bytestx[i]=bt;
            }

            PortingContract pc = new PortingContract();
            Object p = getPortingContract(portingContractID);
            if(p==null) return false;
            pc = (PortingContract)p;

            //TODO: assert
            BigInteger amount = pc.AmountBNB;
            byte[] bnbaddr = pc.CollatBCNAddr;

            for (int i=0; i<20; i++)
            {
                if (output.addr[i]!=bnbaddr[i]) return false;
            }

            if (amount != output.amount || output.denom != "BNB")
                return false;

            byte[] txProofLeafHash = challengeVars.txproof.Range(0, 32);

            return (AreEqual(txProofLeafHash, getLeafHashByTxBytes(bytestx.Take(txb.Length-2))));
        }

	private static BigInteger decodeTimestamp(ulong[] bz, int[] ini_fin)
        {
            if (bz.Length<1) return 0;
            //decode field number + type
            //fieldnum, type, n
            ulong[] value64_n = DecodeUvarint(bz, ini_fin);
            ulong typ = value64_n[0] & 0x07;
            if (typ!=0) return 0;
            ulong fnum = value64_n[0] >> 3;
            if (fnum > 268435456) return 0; // 268435456==(1<<29 - 1)
            
            //slide arr
            ini_fin[0] = ini_fin[0]+(int)value64_n[1];
            
            //decode time
            value64_n = DecodeUvarint(bz, ini_fin);
            
            return (BigInteger)value64_n[0];
        }

    }
}
