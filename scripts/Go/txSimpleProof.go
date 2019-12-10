package main

import (
	"encoding/hex"
	"fmt"
	"github.com/binance-chain/go-sdk/client/rpc"
	ctypes "github.com/binance-chain/go-sdk/common/types"
	"github.com/binance-chain/go-sdk/types"
	cmn "github.com/tendermint/tendermint/libs/common"
	
	v "github.com/tendermint/tendermint/types"
        "time"
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

//TODO: add params PrintVoteSignable

func createPrecommit() *v.Vote {
        return createVote(byte(v.PrecommitType))
}

func createVote(t byte) *v.Vote {
        var stamp, err = time.Parse(v.TimeFormat, "2019-11-23T00:05:10.496216845Z")
        if err != nil {
                panic(err)
        }

        hash, _ := hex.DecodeString("DF47F7857B5828E393F95FE7DDCA2A83D581C4CA9BB8473DBAC07587185D41F8")
        partsHash, _ := hex.DecodeString("0BC9A97061EFE72B48D3C1A3AA15AB5405F52B233E7F627F086F2BC18CD287E4")
        validatorAddr, _ := hex.DecodeString("1175946A48EAA473868A0A6F52E6C66CCAF472EA")

        return &v.Vote{
                Type:      v.SignedMsgType(t),
                Height:    50267548,
                Round:     0,
                Timestamp: stamp,
                BlockID: v.BlockID{
                        Hash: hash,
                        PartsHeader: v.PartSetHeader{
                                Total: 1,
                                Hash:  partsHash,
                        },
                },
                ValidatorAddress: validatorAddr,
                ValidatorIndex:   0,
        }
}

func PrintVoteSignableHex() {
        vote := createPrecommit()
        signBytes := vote.SignBytes("Binance-Chain-Tigris")
        fmt.Println(hex.EncodeToString(signBytes))
}


func main() {
	//init rpc client
	nodeAddr := "tcp://127.0.0.1:27147"
	client := rpc.NewRPCClient(nodeAddr, ctypes.ProdNetwork)

	//getting tx from node
	txHash := "D911AA793757C2FD20EF340E2EBF82180B5A9CAA26FB15269086DE24FD6AF776"
	fmt.Println("txHash", "D911AA793757C2FD20EF340E2EBF82180B5A9CAA26FB15269086DE24FD6AF776")
	bytesTxHash, _ := hex.DecodeString(txHash) 
	restx, _ := client.Tx(bytesTxHash, true)

	//tx block
	txBlockHeight := int64(restx.Height)
	fmt.Println("txBlockHeight: ", txBlockHeight)

	//proof
	//total leafs
	txProofTotal := restx.Proof.Proof.Total
	fmt.Println("txProofTotal: ", txProofTotal)

	//tx leaf index
	txProofIndex := restx.Proof.Proof.Index
	fmt.Println("txProofIndex: ", txProofIndex)

	//tx leaf hash
	txProofLeafHash := restx.Proof.Proof.LeafHash
	fmt.Println("txProofLeafHash: ", txProofLeafHash)

	//merkle path
	txProofAunts := restx.Proof.Proof.Aunts
	fmt.Println("txProofAunts: ", txProofAunts)

	//merkle root
	txProofRootHash := restx.Proof.RootHash
	fmt.Println("txProofRootHash: ", txProofRootHash)

	//getting block info to get data hash
	resBlock, _ := client.Block(&txBlockHeight)
	blockDataHash := resBlock.Block.Header.DataHash
	fmt.Println("blockDataHash: ", blockDataHash)

	//paq <- (txProofRootHash | txProofLeafHash | txProofIndex | txProofTotal | txProofAunts... )
	paq := make([]byte, 0)
	paq = append(paq, txProofRootHash[:]...)
	paq = append(paq, txProofLeafHash[:]...)
	paq = append(paq, []byte{byte(txProofIndex)}...)//Warning: assuming txProofIndex always < byteSize
	paq = append(paq, []byte{byte(txProofTotal)}...)//Warning: assuming txProofTotal always < byteSize
	for i := 0; i < len(txProofAunts); i++ {
		paq = append(paq[:], txProofAunts[i][:]...)
	}

	fmt.Println("________________Send to smart contract________________")
	fmt.Println("_______________________Tx Proof_______________________")
	fmt.Println(paq)
	fmt.Println(hex.EncodeToString(paq))
	fmt.Println("______________________________________________________")


	//block header
	//actual block header
	h := resBlock.Block.Header
	fmt.Println("HeaderHashhhh", h.Hash())

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

	fmt.Println("____________________Encoded header____________________")
	fmt.Println(paqHeader)
	fmt.Println(hex.EncodeToString(paqHeader))
	fmt.Println("______________________________________________________")

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

	fmt.Println("______________________Signatures______________________")
	fmt.Println(paqSignatures)
	fmt.Println(hex.EncodeToString(paqSignatures))
	fmt.Println("______________________________________________________")

}
