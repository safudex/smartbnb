import NeoApi from './neolib';
import assertDefined from './asserts';

const NETWORK = "MainNet";
const { PRIVATE_KEY, CONTRACT_SCRIPTHASH /* BNC_TICKER */ } = process.env;
assertDefined(CONTRACT_SCRIPTHASH, 'CONTRACT_SCRIPTHASH');
assertDefined(PRIVATE_KEY, 'PRIVATE_KEY');

const neoApi = new NeoApi(CONTRACT_SCRIPTHASH, PRIVATE_KEY, NETWORK);

