const fs = require('fs');
const axios = require('axios');
const Neon = require("@cityofzion/neon-js");

type contractparam = {
    type:number,
    value:string | contractparam[]
}

enum CONTRACT_CONSTANTS {
    SLICESLEN = 16,
    STG_TYPE_GENERAL = "GENERAL",
    STG_TYPE_PM = "PM",
    STG_TYPE_POINTMUL_SIMPLE = "SIMPLE",
    STG_TYPE_POINTMUL_MULTI = "MULTI",
    STG_TYPE_SIGNABLEBYTES = "SIGNABLEBYTES",
    OPERATION_SAVE = "savestate"
}

enum ARGS_TYPES { String = "String", Array = "Array", ByteArray = "ByteArray", address = "address", fixed8 = "fixed8", Integer = "Integer"}

type rawstate = {
    signatures:string,
    xs:string,
    ys:string,
    signableBytes:string,
    pres:string,
    presHash:string,
    presHashMod:string,
    txProof:string,
    blockHeader:string,
    txBytes:string
}


async function getTxResult(txid: string, leftAttemps: number){
    let txres = await axios.post(NODE_URL, { jsonrpc: "2.0", id: 5, method: "getapplicationlog", params: [txid] })
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

async function invokeOperation(operation: string, args: any[], gas: number, fees: number, sh: string = CONSTRACT_HASH) {
    try {
        const response = await invoke(operation, args, gas, fees, sh)
        console.log(response.txid)
    } catch (error) {
        console.log("Error: ", error.message)
    }
}

async function invoke(operation: string, args: any[], gas: number, fees: number, sh:string) {
    return Neon.default.doInvoke({
        api: API_NODE, // The API Provider that we rely on for balance and rpc information
        account: ECO_WALLET, // The sending Account
        gas, // Additional GAS for invocation.
        fees,
        script: Neon.default.create.script({ scriptHash:sh, operation, args })
    }).then(res => {return res.response})
}

function pushParams(neonJSParams: any[], type: ARGS_TYPES, value: string | number | any[]) {
    if (type == 'String' || type == 'Array' || type == 'ByteArray')
        neonJSParams.push(Neon.default.create.contractParam(type, value));
    else if (type == 'address' || type == 'fixed8')
        // Decimal fixed 8 seems to break at transition 92233720368.54775807 -> 92233720368.54775808
        neonJSParams.push(Neon.sc.ContractParam.byteArray(value, type));
	else if (type == 'Integer')
        neonJSParams.push(Neon.sc.ContractParam.integer(value));
}

function arrayTo2DArray1(list: any[], howMany: number) {
    var result = []
    var input = list.slice(0)
    while (input[0]) {
        result.push(input.splice(0, howMany))
    }
    return result
}

function sleep(ms: number) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

async function SaveState(raw_state: rawstate){
        var neonJSParams = [];
        
		//0 byte[] porting
        pushParams(neonJSParams, ARGS_TYPES.ByteArray, PORTINGCONTRACTID)

        //1 string type
		pushParams(neonJSParams, ARGS_TYPES.String, CONTRACT_CONSTANTS.STG_TYPE_GENERAL)
        
        //2 ulong[][] pre
        var pres = raw_state.pres.split(" ").map(v => v.split(",").map(Neon.sc.ContractParam.integer))
        pushParams(neonJSParams, ARGS_TYPES.Array, pres);
        
		//3 ulong[][] preshash
        var presHash = raw_state.presHash.split(" ").map(v => v.split(",").map(Neon.sc.ContractParam.integer))
		pushParams(neonJSParams, ARGS_TYPES.Array, presHash);
        
        //4 byte[] txproof
        pushParams(neonJSParams, ARGS_TYPES.ByteArray, raw_state.txProof)
        
		//5 byte[] blockHeader
        pushParams(neonJSParams, ARGS_TYPES.ByteArray, raw_state.blockHeader);

		//6 ulong[] txBytes
        var txBytes = raw_state.txBytes.split(",").map(Neon.sc.ContractParam.integer)
		pushParams(neonJSParams, ARGS_TYPES.Array, txBytes);

        await invokeOperation(CONTRACT_CONSTANTS.OPERATION_SAVE, neonJSParams, 50, 10)

        neonJSParams = []	
        
		//0 byte[] porting
        pushParams(neonJSParams, ARGS_TYPES.ByteArray, PORTINGCONTRACTID)

        //1 string type
        pushParams(neonJSParams, ARGS_TYPES.String, CONTRACT_CONSTANTS.STG_TYPE_PM)

        //2 byte[][] signatures
		var signatures = raw_state.signatures.split(",").map(v => Neon.default.create.contractParam('ByteArray', v))
		pushParams(neonJSParams, ARGS_TYPES.Array, signatures);
        
        //3 BigInteger[] xs
		var xs = raw_state.xs.split(",").map(Neon.sc.ContractParam.integer)
        pushParams(neonJSParams, ARGS_TYPES.Array, xs);
        
		//4 BigInteger[] ys
		var ys = raw_state.ys.split(",").map(Neon.sc.ContractParam.integer)
        pushParams(neonJSParams, ARGS_TYPES.Array, ys);
        
        //5 BigInteger[] presHashMod
		var presHashMod = raw_state.presHashMod.split(",").map(Neon.sc.ContractParam.integer)
		pushParams(neonJSParams, ARGS_TYPES.Array, presHashMod);

        await invokeOperation(CONTRACT_CONSTANTS.OPERATION_SAVE, neonJSParams, 500, 100)

        //SENDING SIGNABLEBYTES IN CHUNKS
        var signableBytes = raw_state.signableBytes.split(",").map(v=>v.match(/.{1,2}/g).map(v=>Neon.sc.ContractParam.integer(parseInt(v, 16))))
        signableBytes.forEach(async (signBytes, index) => {
            neonJSParams = []
            //0 byte[] porting
            pushParams(neonJSParams, ARGS_TYPES.ByteArray, PORTINGCONTRACTID)
            //1 string type
            pushParams(neonJSParams, ARGS_TYPES.String, CONTRACT_CONSTANTS.STG_TYPE_SIGNABLEBYTES)
            //2 string id
            pushParams(neonJSParams, ARGS_TYPES.String, ""+String.fromCharCode(index))
            //3 ulong[] data
            pushParams(neonJSParams, ARGS_TYPES.Array, signBytes)
            await invokeOperation(CONTRACT_CONSTANTS.OPERATION_SAVE, neonJSParams, 500, 100)
        })

        //SENDING POINTMUL IN CHUNKS
        const numSlices = 256/CONTRACT_CONSTANTS.SLICESLEN
        var pointMulData=fs.readFileSync('pointmulsteps', 'utf-8').split("||");
        
        //point mul sb
		var ss_sb = []
		var Ps_sb=[]
		var Qs_sb=[]
		var pointMuls_sb = pointMulData[0].split(" ")
		pointMuls_sb.forEach(v => {
			var all = v.split(",")
			ss_sb = ss_sb.concat(all.slice(0, 32).map(Neon.sc.ContractParam.integer))
			var Qs_tmp = all.slice(32, 32+(32*4)).map(Neon.sc.ContractParam.integer)
			Qs_sb = Qs_sb.concat(arrayTo2DArray1(Qs_tmp, 4))
			var Ps_tmp = all.slice(32+(32*4)).map(Neon.sc.ContractParam.integer)
			Ps_sb = Ps_sb.concat(arrayTo2DArray1(Ps_tmp, 4))
		})

		await savePointMuls(Ps_sb, numSlices, "Ps_sb", CONTRACT_CONSTANTS.STG_TYPE_POINTMUL_MULTI)
		await savePointMuls(ss_sb, numSlices, "ss_sb", CONTRACT_CONSTANTS.STG_TYPE_POINTMUL_SIMPLE)
		await savePointMuls(Qs_sb, numSlices, "Qs_sb", CONTRACT_CONSTANTS.STG_TYPE_POINTMUL_MULTI)

        //point mul ha
		var ss_ha = []
		var Ps_ha=[]
		var Qs_ha=[]
		var pointMuls_ha = pointMulData[1].split(" ")
		pointMuls_ha.forEach(v => {
			var all = v.split(",")
			ss_ha = ss_ha.concat(all.slice(0, 32).map(Neon.sc.ContractParam.integer))
			var Qs_tmp = all.slice(32, 32+(32*4)).map(Neon.sc.ContractParam.integer)
			Qs_ha = Qs_ha.concat(arrayTo2DArray1(Qs_tmp, 4))
			var Ps_tmp = all.slice(32+(32*4)).map(Neon.sc.ContractParam.integer)
			Ps_ha = Ps_ha.concat(arrayTo2DArray1(Ps_tmp, 4))
		})

        await savePointMuls(Ps_ha, numSlices, "Ps_ha", CONTRACT_CONSTANTS.STG_TYPE_POINTMUL_MULTI)
		await savePointMuls(ss_ha, numSlices, "ss_ha", CONTRACT_CONSTANTS.STG_TYPE_POINTMUL_SIMPLE)
		await savePointMuls(Qs_ha, numSlices, "Qs_ha", CONTRACT_CONSTANTS.STG_TYPE_POINTMUL_MULTI)
}

async function savePointMuls(arr: any[], nchks: number, id: string, type: string) {
    var j = arr.length/nchks
    for (var i=0; i<nchks; i++) {
        var neonJSParams = []
        //0 byte[] porting
        pushParams(neonJSParams, ARGS_TYPES.ByteArray, PORTINGCONTRACTID)
        //1 string type
        pushParams(neonJSParams, ARGS_TYPES.String, type)
        //2 string id
        pushParams(neonJSParams, ARGS_TYPES.String, id+String.fromCharCode(i))
        //3 string data
        pushParams(neonJSParams, ARGS_TYPES.Array, arr.slice(i*j, (i+1)*j))
        await invokeOperation(CONTRACT_CONSTANTS.OPERATION_SAVE, neonJSParams, 50, 10)
    }
}

const cmdArgs: string[] = process.argv.slice(2)
var ECO_WALLET = new Neon.wallet.Account(cmdArgs[0]); //ALWAYS CONST????
const NODE_URL = cmdArgs[1]
const API_NODE = new Neon.api.neoCli.instance(NODE_URL);
const PORTINGCONTRACTID = cmdArgs[2]
const CONSTRACT_HASH = cmdArgs[3]

const raw_state = {
    signatures:cmdArgs[4],
    xs:cmdArgs[5],
    ys:cmdArgs[6],
    signableBytes:cmdArgs[7],
    pres:cmdArgs[8],
    presHash:cmdArgs[9],
    presHashMod:cmdArgs[10],
    txProof:cmdArgs[11],
    blockHeader:cmdArgs[12],
    txBytes:cmdArgs[13]
}

SaveState(raw_state)