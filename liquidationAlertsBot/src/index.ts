import Neon, {u, api} from '@cityofzion/neon-js';
import assertDefined from './asserts';
import Emailer from './emailer';
import hex2int from './neoutils';

const NETWORK = 'MainNet';
const {
  CONTRACT_SCRIPTHASH, EMAIL_ADDRESS,
} = process.env;
assertDefined(CONTRACT_SCRIPTHASH, 'CONTRACT_SCRIPTHASH');
assertDefined(EMAIL_ADDRESS, 'EMAIL_ADDRESS');

const emailProvider = new Emailer(EMAIL_ADDRESS, '"Liquidations bot" <liquidation-alerts@smartbnb.net>', 'New smartBNB collateral liquidation');
const notificationsProvider = new api.notifications.instance(NETWORK);
notificationsProvider.subscribe(CONTRACT_SCRIPTHASH, async (event)=>{
  const operation = u.hexstring2str(event.event[0].value);
  if (operation !== "collatliquidated"){
    return; // ignore
  }
  const collatID = u.hexstring2str(event.event[1].value);
  const liquidatedGAS = hex2int(event.event[2]);
  emailProvider.send(`Collateral provider with ID ${collatID} has had it's deposit of ${liquidatedGAS} GAS liquidated.`);
});