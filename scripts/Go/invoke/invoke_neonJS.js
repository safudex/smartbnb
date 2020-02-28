const fs = require('fs');
const axios = require('axios');
const Neon = require("@cityofzion/neon-js");
var ECO_WALLET = new Neon.wallet.Account("KxDgvEKzgSBPPfuVfw67oPQBSjidEiqTHURKSDL1R7yGaGYAeYnr");
var scriptHash = "05966f89303289902c28c39492ba30b75c1867b2" //const
const cmdArgs = process.argv.slice(2)
const node = "https://node"+randomInt(1, 3)+".neocompiler.io"
const api = new Neon.api.neoCli.instance(node);
const txHash = "87E98C672940790460055F807B0AE76C8A88826D542EB1107B6713FB102D2BC6"

async function getTxResult(txid, leftAttemps){
    let txres = await axios.post(node, { jsonrpc: "2.0", id: 5, method: "getapplicationlog", params: [txid] })
    if (txres.data.error && leftAttemps > 0 ) {
        console.log(leftAttemps, txid, txres.data.error)
        sleep(1000)
        txres = await getTxResult(txid, leftAttemps-1)
    }
    else if (leftAttemps < 0) {
        console.log(leftAttemps, txid, txres.data.error)
        return null
    }
    
    return txres
}

async function invokeOperation(operation, args, gas, fees){
    console.log(args.length)

    let txr
    try {
        const response = await invoke(operation, args, gas, fees)
        txr = (await getTxResult(response.txid, 2*60)).data.result.executions[0]
        if (!txr) {
            console.log("Attemps exhausted")
            //retry
        }
        console.log(txr)//return txr
    } catch (error) {
        console.log(error.message)
    }
}

async function invoke(operation, args, gas, fees){
    return Neon.default.doInvoke({
        api, // The API Provider that we rely on for balance and rpc information
        account: ECO_WALLET, // The sending Account
        gas, // Additional GAS for invocation.
        fees,
        script: Neon.default.create.script({ scriptHash, operation, args })
    }).then(res => {return res.response})
}

function pushParams(neonJSParams, type, value, ) {
    if (type == 'String')
        neonJSParams.push(Neon.default.create.contractParam(type, value));
    else if (type == 'Address')
        neonJSParams.push(Neon.sc.ContractParam.byteArray(value, 'address'));
    else if (type == 'Hex')
        neonJSParams.push(Neon.default.create.contractParam('ByteArray', value));
    else if (type == 'DecFixed8')
        // Decimal fixed 8 seems to break at transition 92233720368.54775807 -> 92233720368.54775808
        neonJSParams.push(Neon.sc.ContractParam.byteArray(value, 'fixed8'))
	else if (type == 'Integer')
        neonJSParams.push(Neon.sc.ContractParam.integer(value));
    else if (type == 'Array')
        neonJSParams.push(Neon.default.create.contractParam(type, value));
}

function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

function randomInt(low, high) {
  return Math.floor(Math.random() * (high - low) + low)
}

async function SaveState(){
		var neonJSParams = [];
		//0 byte[] calleraddr
		pushParams(neonJSParams, 'Address', ECO_WALLET._address);
		//1 byte[] txid
		pushParams(neonJSParams, 'Hex', txHash)//cmdArgs[1]);
		//2 byte[][] signatures 
		var sigs = cmdArgs[2].split(",").map(v => Neon.default.create.contractParam('ByteArray', v))
		pushParams(neonJSParams, 'Array', sigs);
		//3 bigint[] xs
		var xs = cmdArgs[3].split(",").map(Neon.sc.ContractParam.integer)
		pushParams(neonJSParams, 'Array', xs);
		//4 bigint[] ys
		var ys = cmdArgs[4].split(",").map(Neon.sc.ContractParam.integer)
		pushParams(neonJSParams, 'Array', ys);
		//5 byte[][] signablebytes
		var signableBytes = cmdArgs[5].split(",").map(v => Neon.default.create.contractParam('ByteArray', v))
		pushParams(neonJSParams, 'Array', signableBytes);
		//6 ulong[][] pres
		var pres = cmdArgs[6].split(" ").map(v => v.split(",").map(Neon.sc.ContractParam.integer))
		pushParams(neonJSParams, 'Array', pres);
		//7 ulong[][] preshash
		var presHash = cmdArgs[7].split(" ").map(v => v.split(",").map(Neon.sc.ContractParam.integer))
		pushParams(neonJSParams, 'Array', presHash);
		//8 bigint[] preshashmod
		var presHashMod = cmdArgs[8].split(",").map(Neon.sc.ContractParam.integer)
		pushParams(neonJSParams, 'Array', presHashMod);
		//9 byte[] txproof
		pushParams(neonJSParams, 'Hex', cmdArgs[9]);
		//10 byte[] blockHeader
		pushParams(neonJSParams, 'Hex', cmdArgs[10]);

        await invokeOperation("savestate", neonJSParams, 50, 10)

        //state pointmul sb
		//12 bigint[][] Qs_sb
		//13 bigint[] ss_sb
		//14 bigint[][] Ps_sb
		var pointMulData=fs.readFileSync('pointmulsteps', 'utf-8').split("||");
		var ss_sb = []
		var Ps_sb=[]
		var Qs_sb=[]
		var pointMuls_sb = pointMulData[0].split(" ")
		pointMuls_sb.map(v => {
			var all = v.split(",")
			ss_sb = ss_sb.concat(all.slice(0, 32).map(Neon.sc.ContractParam.integer))
			var Qs_tmp = all.slice(32, 32+(32*4)).map(Neon.sc.ContractParam.integer)
			Qs_sb = Qs_sb.concat(arrayTo2DArray1(Qs_tmp, 4))
			var Ps_tmp = all.slice(32+(32*4)).map(Neon.sc.ContractParam.integer)
			Ps_sb = Ps_sb.concat(arrayTo2DArray1(Ps_tmp, 4))
		})

		await savePointMuls(Ps_sb, 2, "Ps_sb", "multi")
		await savePointMuls(ss_sb, 2, "ss_sb", "simple")
		await savePointMuls(Qs_sb, 2, "Qs_sb", "multi")

		
		//Second invoke, state pointmul ha
		var ss_ha = []
		var Ps_ha=[]
		var Qs_ha=[]
		var pointMuls_ha = pointMulData[1].split(" ")
		pointMuls_ha.map(v => {
			var all = v.split(",")
			ss_ha = ss_ha.concat(all.slice(0, 32).map(Neon.sc.ContractParam.integer))
			var Qs_tmp = all.slice(32, 32+(32*4)).map(Neon.sc.ContractParam.integer)
			Qs_ha = Qs_ha.concat(arrayTo2DArray1(Qs_tmp, 4))
			var Ps_tmp = all.slice(32+(32*4)).map(Neon.sc.ContractParam.integer)
			Ps_ha = Ps_ha.concat(arrayTo2DArray1(Ps_tmp, 4))
		})

		await savePointMuls(Ps_ha, 2, "Ps_ha", "multi")
		await savePointMuls(ss_ha, 2, "ss_ha", "simple")
		await savePointMuls(Qs_ha, 2, "Qs_ha", "multi")
}

async function savePointMuls(arr, nchks, id, type) {
		var j = arr.length/nchks
		for (var i=0; i<nchks; i++) {
				neonJSParams = []
				//0 byte[] calleraddr
				pushParams(neonJSParams, 'Address', ECO_WALLET._address);
				//1 byte[] txid
				pushParams(neonJSParams, 'Hex', txHash);
				pushParams(neonJSParams, 'String', type)
				pushParams(neonJSParams, 'String', id+i)
				console.log(id+i)
				pushParams(neonJSParams, 'Array', arr.slice(i*j, (i+1)*j))

                await invokeOperation("savestate", neonJSParams, 50, 10)
		}
}

function arrayTo2DArray1(list, howMany) {
    var result = []
    input = list.slice(0)
    while (input[0]) {
        result.push(input.splice(0, howMany))
    }
    return result
}

function Challenge0(){
    var neonJSParams = [];

    //0 byte[] calleraddr
    pushParams(neonJSParams, 'Address', ECO_WALLET._address);
    //1 byte[] txid
    pushParams(neonJSParams, 'Hex', txHash);
    //2 int signature id
    pushParams(neonJSParams, 'Integer', 0);

    invokeOperation("challenge 0", neonJSParams, 50, 10)
}

function Challenge1(){
    var neonJSParams = [];

    //0 byte[] calleraddr
    pushParams(neonJSParams, 'Address', ECO_WALLET._address);
    //1 byte[] txid
    pushParams(neonJSParams, 'Hex', txHash);
    //2 int signature id
    pushParams(neonJSParams, 'Integer', 7);

    invokeOperation("challenge 1", neonJSParams, 50, 10)
}

function Challenge2(){
    var neonJSParams = [];

    //0 byte[] calleraddr
    pushParams(neonJSParams, 'Address', ECO_WALLET._address);
    //1 byte[] txid
    pushParams(neonJSParams, 'Hex', txHash);
    //2 int signature id
    pushParams(neonJSParams, 'Integer', 0);
    invokeOperation("challenge 2", neonJSParams, 50, 10)
}

function Challenge3(){
    var neonJSParams = [];

    //0 byte[] calleraddr
    pushParams(neonJSParams, 'Address', ECO_WALLET._address);
    //1 byte[] txid
    pushParams(neonJSParams, 'Hex', txHash);
    //2 int signature id
    pushParams(neonJSParams, 'Integer', 0);
    
    invokeOperation("challenge 3", neonJSParams, 50, 10)
}

function Challenge4(){
    var neonJSParams = [];

    //0 byte[] calleraddr
    pushParams(neonJSParams, 'Address', ECO_WALLET._address);
    //1 byte[] txid
    pushParams(neonJSParams, 'Hex', txHash);
    //2 int signature id
    pushParams(neonJSParams, 'Integer', 7);
    
    invokeOperation("challenge 4", neonJSParams, 50, 10)
}

function Challenge5(){
    var neonJSParams = [];

    //0 byte[] calleraddr
    pushParams(neonJSParams, 'Address', ECO_WALLET._address);
    //1 byte[] txid
    pushParams(neonJSParams, 'Hex', txHash);
    //2 int signature id
    pushParams(neonJSParams, 'Integer', 4);
    //3 int step num
    pushParams(neonJSParams, 'Integer', 30)
    pushParams(neonJSParams, 'String', "sb");

    invokeOperation("challenge 5", neonJSParams, 50, 10)
}

function Challenge6(){
    var neonJSParams = [];

    //0 byte[] calleraddr
    pushParams(neonJSParams, 'Address', ECO_WALLET._address);
    //1 byte[] txid
    pushParams(neonJSParams, 'Hex', txHash);

    invokeOperation("challenge 6", neonJSParams, 50, 10)

}

function IsSaved(){
    var neonJSParams = [];

    //0 byte[] calleraddr
    pushParams(neonJSParams, 'Address', ECO_WALLET._address);
    //1 byte[] txid
    pushParams(neonJSParams, 'Hex', txHash);

    invokeOperation("proofIsSaved", neonJSParams, 50, 10)
}

async function RemoveStorage(){
    var neonJSParams = [];

    //0 byte[] calleraddr
    pushParams(neonJSParams, 'Address', ECO_WALLET._address);
    //1 byte[] txid
    pushParams(neonJSParams, 'Hex', txHash);
    
    await invokeOperation("removeStorage", neonJSParams, 50, 10)
}
function ActivateChallenge(){
    var neonJSParams = [];

    //0 byte[] calleraddr
    pushParams(neonJSParams, 'Address', ECO_WALLET._address);
    //1 byte[] txid
    pushParams(neonJSParams, 'Hex', txHash);

    invokeOperation("activateChallenge", neonJSParams, 50, 10)
}
function VerifyTxOutput(){
    var neonJSParams = [];
var tx = [216, 1, 240, 98, 93, 238, 10, 76, 42, 44, 135, 250, 10, 34, 10, 20, 128, 235, 113, 44, 199, 136, 156, 221, 198, 42, 67, 190, 33, 134, 58, 181, 169, 92, 237, 173, 18, 10, 10, 3, 66, 78, 66, 16, 128, 225, 235, 23, 18, 34, 10, 20, 210, 70, 25, 113, 160, 90, 159, 247, 12, 180, 104, 194, 51, 29, 152, 47, 90, 15, 172, 236, 18, 10, 10, 3, 66, 78, 66, 16, 128, 225, 235, 23, 18, 112, 10, 38, 235, 90, 233, 135, 33, 3, 67, 169, 180, 183, 13, 156, 59, 217, 155, 188, 133, 225, 125, 2, 42, 73, 159, 171, 57, 13, 26, 83, 254, 126, 192, 134, 226, 40, 115, 10, 144, 200, 18, 64, 114, 173, 138, 139, 201, 232, 144, 246, 126, 203, 107, 21, 157, 27, 184, 35, 244, 240, 138, 253, 28, 155, 245, 194, 52, 178, 14, 130, 47, 66, 84, 46, 115, 150, 19, 225, 2, 38, 237, 86, 164, 138, 15, 31, 223, 138, 203, 31, 72, 206, 1, 231, 41, 197, 143, 67, 129, 246, 147, 239, 40, 144, 121, 29, 24, 188, 153, 9, 32, 12, 26, 16, 52, 55, 55, 57, 52, 57, 54, 54, 51, 54, 53, 57, 53, 51, 51, 54, 32, 1]

// tx = [34, 10, 20, 210, 70, 25, 113, 160, 90, 159, 247, 12, 180, 104, 194, 51, 29, 152, 47, 90, 15, 172, 236, 18, 10, 10, 3, 66, 78, 66, 16, 128, 225, 235, 23]

tx = [210, 1, 240, 98, 93, 238, 10, 76, 42, 44, 135, 250, 10, 34, 10, 20, 181, 101, 237, 121, 59, 153, 236, 195, 12, 47, 165, 176, 68, 132, 118, 63, 127, 188, 46, 229, 18, 10, 10, 3, 66, 78, 66, 16, 192, 135, 190, 41, 18, 34, 10, 20, 142, 167, 13, 125, 46, 168, 161, 75, 162, 179, 61, 24, 213, 223, 189, 111, 174, 10, 110, 168, 18, 10, 10, 3, 66, 78, 66, 16, 192, 135, 190, 41, 18, 113, 10, 38, 235, 90, 233, 135, 33, 3, 205, 25, 80, 103, 5, 90, 173, 246, 22, 123, 207, 204, 240, 22, 30, 91, 36, 164, 227, 132, 43, 174, 172, 89, 72, 82, 2, 100, 145, 140, 92, 104, 18, 64, 64, 162, 231, 240, 255, 245, 20, 96, 13, 22, 19, 87, 112, 113, 22, 35, 238, 90, 58, 184, 162, 45, 183, 127, 223, 168, 210, 37, 179, 142, 57, 253, 13, 51, 177, 234, 162, 190, 32, 134, 242, 111, 108, 131, 200, 77, 132, 111, 234, 212, 225, 40, 172, 66, 191, 137, 222, 197, 124, 159, 244, 45, 0, 59, 24, 232, 237, 3, 32, 147, 4, 26, 9, 49, 48, 54, 49, 48, 57, 56, 54, 51, 32, 2]

tx = tx.map(Neon.sc.ContractParam.integer)
console.log("len", tx.length)
pushParams(neonJSParams, 'Array', tx)
pushParams(neonJSParams, 'Integer', 49)
pushParams(neonJSParams, 'Integer', 35)

    //0 byte[] calleraddr
//    pushParams(neonJSParams, 'Address', ECO_WALLET._address);
    //1 byte[] txid
//    pushParams(neonJSParams, 'Hex', txHash);

    invokeOperation("", neonJSParams, 50, 10)

}

scriptHash = "3fe3a10fb642920282ce48f3e8eec47155b9cfbf"

//SaveState()
//Challenge2()
//IsSaved()
//RemoveStorage()
VerifyTxOutput();
