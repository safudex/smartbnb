const fs = require('fs');
const axios = require('axios');
const Neon = require("@cityofzion/neon-js");

const CONSTRACT_HASH = "c1384e12e410cc6a58677324b5ac14ad255684e4" //const

// CONTRACT CONSTANTS
const SLICESLEN = 16
const STG_TYPE_GENERAL = "GENERAL";
const STG_TYPE_PM = "PM";
const STG_TYPE_POINTMUL_SIMPLE = "SIMPLE";
const STG_TYPE_POINTMUL_MULTI = "MULTI";
const STG_TYPE_SIGNABLEBYTES = "SIGNABLEBYTES";

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

async function invokeOperation(operation, args, gas, fees, sh=CONSTRACT_HASH){

    let txr
    try {
        const response = await invoke(operation, args, gas, fees, sh)
        txr = (await getTxResult(response.txid, 2*60)).data.result//.executions[0]
        if (!txr) {
            console.log("Attemps exhausted")
            //retry
        }
        else{
            console.log(txr.executions[0])//return txr
            txr.executions[0].notifications.forEach(el => {
                console.log(el.state.value[0].value)
            });
        }
    } catch (error) {
        console.log(error.message)
    }
}

async function invoke(operation, args, gas, fees, sh){
    return Neon.default.doInvoke({
        api: API_NODE, // The API Provider that we rely on for balance and rpc information
        account: ECO_WALLET, // The sending Account
        gas, // Additional GAS for invocation.
        fees,
        script: Neon.default.create.script({ scriptHash:sh, operation, args })
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

function arrayTo2DArray1(list, howMany) {
    var result = []
    var input = list.slice(0)
    while (input[0]) {
        result.push(input.splice(0, howMany))
    }
    return result
}

async function SaveState(raw_state){
        var neonJSParams = [];
        
		//0 byte[] porting
        pushParams(neonJSParams, 'Hex', PORTINGCONTRACTID)

        //1 string type
		pushParams(neonJSParams, 'String', STG_TYPE_GENERAL)
        
        //2 ulong[][] pre
        var pres = raw_state.pres.split(" ").map(v => v.split(",").map(Neon.sc.ContractParam.integer))
        pushParams(neonJSParams, 'Array', pres);
        
		//3 ulong[][] preshash
        var presHash = raw_state.presHash.split(" ").map(v => v.split(",").map(Neon.sc.ContractParam.integer))
		pushParams(neonJSParams, 'Array', presHash);
        
        //4 byte[] txproof
        pushParams(neonJSParams, 'Hex', raw_state.txProof)
        
		//5 byte[] blockHeader
        pushParams(neonJSParams, 'Hex', raw_state.blockHeader);

		//6 ulong[] txBytes
        var txBytes = raw_state.txBytes.split(",").map(Neon.sc.ContractParam.integer)
		pushParams(neonJSParams, 'Array', txBytes);

        await invokeOperation("savestate", neonJSParams, 50, 10)

        neonJSParams = []	
        
		//0 byte[] porting
        pushParams(neonJSParams, 'Hex', PORTINGCONTRACTID)

        //1 string type
        pushParams(neonJSParams, 'String', STG_TYPE_PM)

        //2 byte[][] signatures
		var signatures = raw_state.signatures.split(",").map(v => Neon.default.create.contractParam('ByteArray', v))
		pushParams(neonJSParams, 'Array', signatures);
        
        //3 BigInteger[] xs
		var xs = raw_state.xs.split(",").map(Neon.sc.ContractParam.integer)
        pushParams(neonJSParams, 'Array', xs);
        
		//4 BigInteger[] ys
		var ys = raw_state.ys.split(",").map(Neon.sc.ContractParam.integer)
        pushParams(neonJSParams, 'Array', ys);
        
        //5 BigInteger[] presHashMod
		var presHashMod = raw_state.presHashMod.split(",").map(Neon.sc.ContractParam.integer)
		pushParams(neonJSParams, 'Array', presHashMod);

        await invokeOperation("savestate", neonJSParams, 500, 100)

        //SENDING SIGNABLEBYTES IN CHUNKS
        var signableBytes = raw_state.signableBytes.split(",").map(v=>v.match(/.{1,2}/g).map(v=>Neon.sc.ContractParam.integer(parseInt(v, 16))))
        signableBytes.forEach(async (signBytes, index) => {
            neonJSParams = []
            //0 byte[] porting
            pushParams(neonJSParams, 'Hex', PORTINGCONTRACTID)
            //1 string type
            pushParams(neonJSParams, 'String', STG_TYPE_SIGNABLEBYTES)
            //2 string id
            pushParams(neonJSParams, 'String', ""+String.fromCharCode(index))
            //3 ulong[] data
            pushParams(neonJSParams, 'Array', signBytes)
            await invokeOperation("savestate", neonJSParams, 500, 100)
        })

        //SENDING POINTMUL IN CHUNKS
        const numSlices = 256/SLICESLEN
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

		await savePointMuls(Ps_sb, numSlices, "Ps_sb", STG_TYPE_POINTMUL_MULTI)
		await savePointMuls(ss_sb, numSlices, "ss_sb", STG_TYPE_POINTMUL_SIMPLE)
		await savePointMuls(Qs_sb, numSlices, "Qs_sb", STG_TYPE_POINTMUL_MULTI)

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

        await savePointMuls(Ps_ha, numSlices, "Ps_ha", STG_TYPE_POINTMUL_MULTI)
		await savePointMuls(ss_ha, numSlices, "ss_ha", STG_TYPE_POINTMUL_SIMPLE)
		await savePointMuls(Qs_ha, numSlices, "Qs_ha", STG_TYPE_POINTMUL_MULTI)
}

async function savePointMuls(arr, nchks, id, type) {
    var j = arr.length/nchks
    for (var i=0; i<nchks; i++) {
        var neonJSParams = []
        //0 byte[] porting
        pushParams(neonJSParams, 'Hex', PORTINGCONTRACTID)
        //1 string type
        pushParams(neonJSParams, 'String', type)
        //2 string id
        pushParams(neonJSParams, 'String', id+String.fromCharCode(i))
        //3 string data
        pushParams(neonJSParams, 'Array', arr.slice(i*j, (i+1)*j))
        await invokeOperation("savestate", neonJSParams, 50, 10)
    }
}

const cmdArgs = process.argv.slice(2)
var ECO_WALLET = new Neon.wallet.Account(cmdArgs[0]); //ALWAYS CONST????
const API_NODE = new Neon.api.neoCli.instance(cmdArgs[1]);
const PORTINGCONTRACTID = cmdArgs[2]

const raw_state = {
    signatures:cmdArgs[3],
    xs:cmdArgs[4],
    ys:cmdArgs[5],
    signableBytes:cmdArgs[6],
    pres:cmdArgs[7],
    presHash:cmdArgs[8],
    presHashMod:cmdArgs[9],
    txProof:cmdArgs[10],
    blockHeader:cmdArgs[11],
    txBytes:cmdArgs[12],
}

SaveState(raw_state)