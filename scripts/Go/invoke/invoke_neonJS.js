const fs = require('fs');
const axios = require('axios');
const Neon = require("@cityofzion/neon-js");
var ECO_WALLET = new Neon.wallet.Account("KxDgvEKzgSBPPfuVfw67oPQBSjidEiqTHURKSDL1R7yGaGYAeYnr");
var scriptHash = "05966f89303289902c28c39492ba30b75c1867b2" //const
const cmdArgs = process.argv.slice(2)
const node = "https://node"+randomInt(1, 3)+".neocompiler.io"
const api = new Neon.api.neoCli.instance(node);
const txHash = cmdArgs[1]

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
		//1 byte[] porting
		pushParams(neonJSParams, 'Hex', portingContractID)
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
		//11 byte[] txbytes
		console.log("___________________", cmdArgs[11])
		var t = cmdArgs[11].split(",").map(Neon.sc.ContractParam.integer)
		pushParams(neonJSParams, 'Array', t);

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
				pushParams(neonJSParams, 'Hex', portingContractID)
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
    //pushParams(neonJSParams, 'Address', ECO_WALLET._address);
    //1 byte[] txid
    //pushParams(neonJSParams, 'Hex', txHash);
				pushParams(neonJSParams, 'Hex', portingContractID)
    //2 int signature id
    pushParams(neonJSParams, 'Integer', 0);

    invokeOperation("challenge 0", neonJSParams, 50, 10)
}

function Challenge1(){
    var neonJSParams = [];

    //0 byte[] calleraddr
    //pushParams(neonJSParams, 'Address', ECO_WALLET._address);
    //1 byte[] txid
    //pushParams(neonJSParams, 'Hex', txHash);
	pushParams(neonJSParams, 'Hex', portingContractID)
    //2 int signature id
    pushParams(neonJSParams, 'Integer', 7);

    invokeOperation("challenge 1", neonJSParams, 50, 10)
}

function Challenge2(){
    var neonJSParams = [];

    //0 byte[] calleraddr
   // pushParams(neonJSParams, 'Address', ECO_WALLET._address);
    //1 byte[] txid
   // pushParams(neonJSParams, 'Hex', txHash);
	pushParams(neonJSParams, 'Hex', portingContractID)
    //2 int signature id
    pushParams(neonJSParams, 'Integer', 0);
    invokeOperation("challenge 2", neonJSParams, 50, 10)
}

function Challenge3(){
    var neonJSParams = [];

    //0 byte[] calleraddr
    //pushParams(neonJSParams, 'Address', ECO_WALLET._address);
    //1 byte[] txid
    //pushParams(neonJSParams, 'Hex', txHash);
	pushParams(neonJSParams, 'Hex', portingContractID)
    //2 int signature id
    pushParams(neonJSParams, 'Integer', 0);
    
    invokeOperation("challenge 3", neonJSParams, 50, 10)
}

function Challenge4(){
    var neonJSParams = [];

    //0 byte[] calleraddr
    //pushParams(neonJSParams, 'Address', ECO_WALLET._address);
    //1 byte[] txid
    //pushParams(neonJSParams, 'Hex', txHash);
	pushParams(neonJSParams, 'Hex', portingContractID)
    //2 int signature id
    pushParams(neonJSParams, 'Integer', 7);
    
    invokeOperation("challenge 4", neonJSParams, 50, 10)
}

function Challenge5(){
    var neonJSParams = [];

    //0 byte[] calleraddr
    //pushParams(neonJSParams, 'Address', ECO_WALLET._address);
    //1 byte[] txid
    //pushParams(neonJSParams, 'Hex', txHash);
	pushParams(neonJSParams, 'Hex', portingContractID)
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
    //pushParams(neonJSParams, 'Address', ECO_WALLET._address);
    //1 byte[] txid
    //pushParams(neonJSParams, 'Hex', txHash);
	pushParams(neonJSParams, 'Hex', portingContractID)

    invokeOperation("challenge 6", neonJSParams, 50, 10)

}

function Challenge7(){
    var neonJSParams = [];

    //0 byte[] calleraddr
    //pushParams(neonJSParams, 'Address', ECO_WALLET._address);
    //1 byte[] txid
    //pushParams(neonJSParams, 'Hex', txHash);
	pushParams(neonJSParams, 'Hex', portingContractID)

    invokeOperation("challenge 7", neonJSParams, 150, 10)
}

function IsSaved(){
    var neonJSParams = [];

    //0 byte[] calleraddr
    //pushParams(neonJSParams, 'Address', ECO_WALLET._address);
    //1 byte[] txid
    //pushParams(neonJSParams, 'Hex', txHash);
	pushParams(neonJSParams, 'Hex', portingContractID)

    invokeOperation("proofIsSaved", neonJSParams, 50, 10)
}

function RegisterAsCollateral(){
    var neonJSParams = [];

    //0 byte[] collataddr
    pushParams(neonJSParams, 'Address', ECO_WALLET._address);
    //1 byte[] bncaddress
    var bncaddr = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
    pushParams(neonJSParams, 'Hex', bncaddr);
    pushParams(neonJSParams, 'Integer', 300)
    pushParams(neonJSParams, 'Integer', 6)

    invokeOperation("registerAsCollateral", neonJSParams, 50, 10)
}

function NewPorting(){
    var neonJSParams = [];

    //0 byte[] collatid 
    pushParams(neonJSParams, 'Hex', collatid);
    //1 byte[] portingContractID
    pushParams(neonJSParams, 'Address', ECO_WALLET._address);
    pushParams(neonJSParams, 'Integer', 40)

    invokeOperation("newPorting", neonJSParams, 50, 10)
}

function ackUserDepositPorting(){
    var neonJSParams = [];

    //0 byte[] collatid 
    pushParams(neonJSParams, 'Hex', portingContractID);
    //1 byte[] portingContractID
    pushParams(neonJSParams, 'Address', ECO_WALLET._address);

    invokeOperation("ackDepositByUser", neonJSParams, 50, 10)
}

function executeChallenge(){
    var neonJSParams = [];

    //0 byte[] collatid 
//    pushParams(neonJSParams, 'Hex', portingContractID);
    //1 byte[] portingContractID
  //  pushParams(neonJSParams, 'Address', ECO_WALLET._address);
	pushParams(neonJSParams, 'Hex', portingContractID)
    pushParams(neonJSParams, 'Integer', 0)
    pushParams(neonJSParams, 'Integer', 0)

    invokeOperation("executeChallenge", neonJSParams, 50, 10)
}

function challengeDeposit(){
    var neonJSParams = [];

    //0 byte[] collatid 
//    pushParams(neonJSParams, 'Hex', portingContractID);
    //1 byte[] portingContractID
  //  pushParams(neonJSParams, 'Address', ECO_WALLET._address);
    pushParams(neonJSParams, 'Hex', portingContractID)

    invokeOperation("challengedeposit", neonJSParams, 50, 10)
}

function challengeWithdraw(){
    var neonJSParams = [];

    //0 byte[] collatid 
//    pushParams(neonJSParams, 'Hex', portingContractID);
    //1 byte[] portingContractID
  //  pushParams(neonJSParams, 'Address', ECO_WALLET._address);
    pushParams(neonJSParams, 'Hex', portingContractID)

    invokeOperation("challengewithdraw", neonJSParams, 50, 10)
}

function requestWithdraw(){
    var neonJSParams = [];

    //0 byte[] collatid 
//    pushParams(neonJSParams, 'Hex', portingContractID);
    //1 byte[] portingContractID
  //  pushParams(neonJSParams, 'Addrdess', ECO_WALLET._address);
    pushParams(neonJSParams, 'Hex', portingContractID)

    invokeOperation("requestwithdraw", neonJSParams, 50, 10)
}

function unlockCollateral(){
    var neonJSParams = [];

    pushParams(neonJSParams, 'Hex', portingContractID)

    invokeOperation("unlockcollateral", neonJSParams, 50, 10)
}

collatid = "23ba2703c53263e8d6e522dc32203339dcd8eee9aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
portingContractID = "23ba2703c53263e8d6e522dc32203339dcd8eee9aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa23ba2703c53263e8d6e522dc32203339dcd8eee9dde15a5e"
scriptHash = "f70a8006ee7f5e4ee07ce92586d380d81bd2744e"
console.log(ECO_WALLET._address)

//RegisterAsCollateral()
//NewPorting()
//ackUserDepositPorting()
//unlockCollateral()
//SaveState()
requestWithdraw()
//challengeDeposit()
//Challenge7()
//IsSaved()
//RemoveStorage()
//VerifyTxOutput();
