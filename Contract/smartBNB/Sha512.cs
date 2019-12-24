using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework;
using System;
using System.Numerics;

namespace Neo.SmartContract
{
    public class HelloWorld : Framework.SmartContract
    {
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
        
        public static bool Main()
        {
            //byte[] byteMod = {0x0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10};
        //BigInteger mod = byteMod.AsBigInteger();
        byte[] byteV = {0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00};
        ulong v = (ulong)byteV.AsBigInteger();
        ulong[] pre = { 7017280570803617792, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 24 };
        //ulong[] H = { 7640891576956012808, 13503953896175478587, 4354685564936845355, 11912009170470909681, 5840696475078001361, 11170449401992604703, 2270897969802886507, 6620516959819538809 };
        ulong[] W = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        for (int i = 0; i < 16; i++)
        {
            W[i] = pre[i];
        }
        for (int i = 16; i < 80; i++)
        {
            W[i] = (sig1(W[i - 2]) + W[i - 7] + sig0(W[i - 15]) + W[i - 16])&v;
        }

        //ulong[] K = { 4794697086780616226, 8158064640168781261, 13096744586834688815, 16840607885511220156, 4131703408338449720, 6480981068601479193, 10538285296894168987, 12329834152419229976, 15566598209576043074, 1334009975649890238, 2608012711638119052, 6128411473006802146, 8268148722764581231, 9286055187155687089, 11230858885718282805, 13951009754708518548, 16472876342353939154, 17275323862435702243, 1135362057144423861, 2597628984639134821, 3308224258029322869, 5365058923640841347, 6679025012923562964, 8573033837759648693, 10970295158949994411, 12119686244451234320, 12683024718118986047, 13788192230050041572, 14330467153632333762, 15395433587784984357, 489312712824947311, 1452737877330783856, 2861767655752347644, 3322285676063803686, 5560940570517711597, 5996557281743188959, 7280758554555802590, 8532644243296465576, 9350256976987008742, 10552545826968843579, 11727347734174303076, 12113106623233404929, 14000437183269869457, 14369950271660146224, 15101387698204529176, 15463397548674623760, 17586052441742319658, 1182934255886127544, 1847814050463011016, 2177327727835720531, 2830643537854262169, 3796741975233480872, 4115178125766777443, 5681478168544905931, 6601373596472566643, 7507060721942968483, 8399075790359081724, 8693463985226723168, 9568029438360202098, 10144078919501101548, 10430055236837252648, 11840083180663258601, 13761210420658862357, 14299343276471374635, 14566680578165727644, 15097957966210449927, 16922976911328602910, 17689382322260857208, 500013540394364858, 748580250866718886, 1242879168328830382, 1977374033974150939, 2944078676154940804, 3659926193048069267, 4368137639120453308, 4836135668995329356, 5532061633213252278, 6448918945643986474, 6902733635092675308, 7801388544844847127 };
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

        ulong[] H = new ulong[16];
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
        
            return false;
        }

    }
}
