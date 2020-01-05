package proof

import (
	"encoding/hex"
	"github.com/binance-chain/go-sdk/client/rpc"
	ctypes "github.com/binance-chain/go-sdk/common/types"
	"github.com/binance-chain/go-sdk/types"
	cmn "github.com/tendermint/tendermint/libs/common"
	v "github.com/tendermint/tendermint/types"
        "time"
	"os/exec"
	"strings"
)

var cdc = types.NewCodec()

// cdcEncode returns nil if the input is nil, otherwise returns
// cdc.MustMarshalBinaryBare(item)
func cdcEncode(item interface{}) []byte {
	if item != nil && !cmn.IsTypedNil(item) && !cmn.IsEmpty(item) {
		return cdc.MustMarshalBinaryBare(item)
	}
	return nil
}

func createPrecommit(vd voteData) *v.Vote {
        return createVote(vd)
}
type voteData struct {
	timestamp time.Time//string
	hashBlock []byte
	partsHash []byte//string//TODO: ARRAY
	partsTotal int
	validatorAddr []byte
	validatorIndex int
	height int64
	round int
}
func createVote(vd voteData) *v.Vote {
	stamp := vd.timestamp

        return &v.Vote{
                Type:      v.SignedMsgType(byte(v.PrecommitType)),
                Height:    vd.height,
                Round:     vd.round,
                Timestamp: stamp,
                BlockID: v.BlockID{
                        Hash: vd.hashBlock,
                        PartsHeader: v.PartSetHeader{
                                Total: vd.partsTotal,
                                Hash:  vd.partsHash,
                        },
                },
                ValidatorAddress: vd.validatorAddr,
                ValidatorIndex:   0,
        }
}

func VoteSignableHexBytes(vd voteData) string {
        vote := createPrecommit(vd)
        signBytes := vote.SignBytes("Binance-Chain-Tigris")
        return hex.EncodeToString(signBytes)
}

func Decompress(s string) (string, string, string) {
	out, err := exec.Command("python3", "helper.py", "1", s).Output()
	if err != nil {
		panic(err)
	}
	r := strings.Split(string(out), "\n")
	return r[0], r[1], r[2]
}



func GetPreprocessedMsg(msg string) string {
	out, err := exec.Command("python3", "helper.py", "2", msg).Output()
	if err != nil {
		panic(err)
	}
	r := strings.Split(string(out), "\n")
	return r[0]//, r[1], r[2]
	//return string(out)
}

type SPV struct {
	TxProof string
	HeaderHash string
	Header string
	Signatures string
	XSigLow string
	YSigLow string
	PreMsg string
	Msg string
	SignBytes string
}

func GetProof(txHash string) SPV {
	spv := SPV{}
	//init rpc client
	nodeAddr := "http://127.0.0.1:27147"
	client := rpc.NewRPCClient(nodeAddr, ctypes.ProdNetwork)
	//getting tx from node
	bytesTxHash, _ := hex.DecodeString(txHash) 
	restx, _ := client.Tx(bytesTxHash, true)

	//tx block
	txBlockHeight := int64(restx.Height)

	//proof
	//total leafs
	txProofTotal := restx.Proof.Proof.Total

	//tx leaf index
	txProofIndex := restx.Proof.Proof.Index

	//tx leaf hash
	txProofLeafHash := restx.Proof.Proof.LeafHash

	//merkle path
	txProofAunts := restx.Proof.Proof.Aunts

	//merkle root
	txProofRootHash := restx.Proof.RootHash

	//getting block info to get data hash
	resBlock, _ := client.Block(&txBlockHeight)
	//paq <- (txProofRootHash | txProofLeafHash | txProofIndex | txProofTotal | txProofAunts... )
	paq := make([]byte, 0)
	paq = append(paq, txProofRootHash[:]...)
	paq = append(paq, txProofLeafHash[:]...)
	paq = append(paq, []byte{byte(txProofIndex)}...)//Warning: assuming txProofIndex always < byteSize
	paq = append(paq, []byte{byte(txProofTotal)}...)//Warning: assuming txProofTotal always < byteSize
	for i := 0; i < len(txProofAunts); i++ {
		paq = append(paq[:], txProofAunts[i][:]...)
	}

	spv.TxProof = hex.EncodeToString(paq)


	//block header
	//actual block header
	h := resBlock.Block.Header

	hVersion := cdcEncode(h.Version)
	hChainID := cdcEncode(h.ChainID)
	hHeight := cdcEncode(h.Height)
	hTime := cdcEncode(h.Time)
	hNumTxs := cdcEncode(h.NumTxs)
	hTotalTxs := cdcEncode(h.TotalTxs)
	hLastBlockID := cdcEncode(h.LastBlockID)
	hLastCommitHash := cdcEncode(h.LastCommitHash)
	hDataHash := cdcEncode(h.DataHash)
	hValidatorsHash := cdcEncode(h.ValidatorsHash)
	hNextValidatorsHash := cdcEncode(h.NextValidatorsHash)
	hConsensusHash := cdcEncode(h.ConsensusHash)
	hAppHash := cdcEncode(h.AppHash)
	hLastResultsHash := cdcEncode(h.LastResultsHash)
	hEvidenceHash := cdcEncode(h.EvidenceHash)
	hProposerAddress := cdcEncode(h.ProposerAddress)

	var headerArray = [16][]byte {
		hVersion,
		hChainID,
		hHeight,
		hTime,
		hNumTxs,
		hTotalTxs,
		hLastBlockID,
		hLastCommitHash,
		hDataHash,
		hValidatorsHash,
		hNextValidatorsHash,
		hConsensusHash,
		hAppHash,
		hLastResultsHash,
		hEvidenceHash,
		hProposerAddress}

        //paqHeader <- (len(hVersion) | hVersion | len(hChainID) | hChainID | ... | len(hProposerAddress) | hProposerAddress )
	paqHeader := make([]byte, 0)
	for i:=0; i<len(headerArray);i++{
		paqHeader = append(paqHeader, byte(len(headerArray[i])))
		paqHeader = append(paqHeader, headerArray[i]...)
	}

	spv.Header = hex.EncodeToString(paqHeader)

	//signatures
	//signatures from height+1
	nextBlockHeight := txBlockHeight+int64(1)
	resNextBlock, _ := client.Block(&nextBlockHeight) //get height+1 to obtain signatures of height
	s := resNextBlock.Block.LastCommit.Precommits

        //paqSignatures <- (signature0 | signature1 | signature2 | ... | signature10 )
	paqSignatures := make([]byte, 0)
	for i:=0; i<len(s);i++{
		paqSignatures = append(paqSignatures, s[i].Signature...)
	}

	spv.Signatures = hex.EncodeToString(paqSignatures)

	sig_low := hex.EncodeToString(s[0].Signature[:32])

	vote := voteData{
		timestamp: s[0].Timestamp,
		hashBlock: s[0].BlockID.Hash,
		partsHash: s[0].BlockID.PartsHeader.Hash,
		partsTotal: s[0].BlockID.PartsHeader.Total,
		validatorAddr: s[0].ValidatorAddress,
		validatorIndex: s[0].ValidatorIndex,
		height: int64(s[0].Height),
		round: s[0].Round,
		}

	pubks := "d3769d8a1f78b4c17a965f7a30d4181fabbd1f969f46d3c8e83b5ad4845421d8"

	s_x, s_y, _ := Decompress(sig_low)
	spv.XSigLow = s_x
	spv.YSigLow = s_y



	signBytes := VoteSignableHexBytes(vote)
	msg := sig_low+pubks+signBytes
	pre := GetPreprocessedMsg(msg)

	spv.SignBytes = signBytes
	spv.Msg = msg
	spv.PreMsg = pre
	return spv
}
