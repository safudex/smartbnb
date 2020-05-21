import NeoApi from './neolib';
import neoEventHandler from './events';
import assertDefined from './asserts';
import BncApi from './bnclib';

const NETWORK = 'MainNet';
const {
  NEO_PRIVATE_KEY, CONTRACT_SCRIPTHASH, REDIS_URL, BNC_PRIVATE_KEY, BNC_ASSET, EMAIL_ADDRESS,
} = process.env;
// A bit wordy but it can't be done in a loop because Typescript doesn't recognize the type assertions in that case
assertDefined(CONTRACT_SCRIPTHASH, 'CONTRACT_SCRIPTHASH');
assertDefined(NEO_PRIVATE_KEY, 'NEO_PRIVATE_KEY');
assertDefined(BNC_PRIVATE_KEY, 'BNC_PRIVATE_KEY');
assertDefined(REDIS_URL, 'REDIS_URL');
assertDefined(BNC_ASSET, 'BNC_ASSET');
assertDefined(EMAIL_ADDRESS, 'EMAIL_ADDRESS');

const neoApi = new NeoApi(CONTRACT_SCRIPTHASH, NEO_PRIVATE_KEY, NETWORK);
const bncApi = new BncApi(BNC_PRIVATE_KEY, NETWORK, BNC_ASSET);
const collatId = neoApi.address + bncApi.address;
neoApi.subscribe(neoEventHandler(neoApi, REDIS_URL, collatId, bncApi, EMAIL_ADDRESS));
