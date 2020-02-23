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

async function invokeOperation(operation, args, gas){
    let txr
    try {
        const response = await invoke(operation, args, gas)
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

async function invoke(operation, args, gas){
    return Neon.default.doInvoke({
        api, // The API Provider that we rely on for balance and rpc information
        account: ECO_WALLET, // The sending Account
        gas, // Additional GAS for invocation.
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

        await invokeOperation("savestate", neonJSParams, 50)

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
		neonJSParams = []
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

                await invokeOperation("savestate", neonJSParams, 50)
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

    invokeOperation("challenge 0", neonJSParams, 50)
}

function Challenge1(){
    var neonJSParams = [];

    //0 byte[] calleraddr
    pushParams(neonJSParams, 'Address', ECO_WALLET._address);
    //1 byte[] txid
    pushParams(neonJSParams, 'Hex', txHash);
    //2 int signature id
    pushParams(neonJSParams, 'Integer', 7);

    invokeOperation("challenge 1", neonJSParams, 50)
}

function Challenge2(){
    var neonJSParams = [];

    //0 byte[] calleraddr
    pushParams(neonJSParams, 'Address', ECO_WALLET._address);
    //1 byte[] txid
    pushParams(neonJSParams, 'Hex', txHash);
    //2 int signature id
    pushParams(neonJSParams, 'Integer', 0);
    invokeOperation("challenge 2", neonJSParams, 50)
}

function Challenge3(){
    var neonJSParams = [];

    //0 byte[] calleraddr
    pushParams(neonJSParams, 'Address', ECO_WALLET._address);
    //1 byte[] txid
    pushParams(neonJSParams, 'Hex', txHash);
    //2 int signature id
    pushParams(neonJSParams, 'Integer', 0);
    
    invokeOperation("challenge 3", neonJSParams, 50)
}

function Challenge4(){
    var neonJSParams = [];

    //0 byte[] calleraddr
    pushParams(neonJSParams, 'Address', ECO_WALLET._address);
    //1 byte[] txid
    pushParams(neonJSParams, 'Hex', txHash);
    //2 int signature id
    pushParams(neonJSParams, 'Integer', 7);
    
    invokeOperation("challenge 4", neonJSParams, 50)
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

    invokeOperation("challenge 5", neonJSParams, 50)
}

function Challenge6(){
    var neonJSParams = [];

    //0 byte[] calleraddr
    pushParams(neonJSParams, 'Address', ECO_WALLET._address);
    //1 byte[] txid
    pushParams(neonJSParams, 'Hex', txHash);

    invokeOperation("challenge 6", neonJSParams, 50)

}

function IsSaved(){
    var neonJSParams = [];

    //0 byte[] calleraddr
    pushParams(neonJSParams, 'Address', ECO_WALLET._address);
    //1 byte[] txid
    pushParams(neonJSParams, 'Hex', txHash);

    invokeOperation("proofIsSaved", neonJSParams, 50)
}

async function RemoveStorage(){
    var neonJSParams = [];

    //0 byte[] calleraddr
    pushParams(neonJSParams, 'Address', ECO_WALLET._address);
    //1 byte[] txid
    pushParams(neonJSParams, 'Hex', txHash);
    
    await invokeOperation("removeStorage", neonJSParams, 50)
}
function ActivateChallenge(){
    var neonJSParams = [];

    //0 byte[] calleraddr
    pushParams(neonJSParams, 'Address', ECO_WALLET._address);
    //1 byte[] txid
    pushParams(neonJSParams, 'Hex', txHash);

    invokeOperation("activateChallenge", neonJSParams, 50)
}

scriptHash = "05966f89303289902c28c39492ba30b75c1867b2"

//SaveState()
//Challenge2()
IsSaved()
//RemoveStorage()
