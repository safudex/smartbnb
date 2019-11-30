package main

import (
    "encoding/hex"
    "fmt"
    "github.com/binance-chain/go-sdk/client/rpc"
    ctypes "github.com/binance-chain/go-sdk/common/types"
    "github.com/binance-chain/go-sdk/types"
	cmn "github.com/tendermint/tendermint/libs/common"
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

func main() {
	//init rpc client
	nodeAddr := "tcp://127.0.0.1:27147"
	client := rpc.NewRPCClient(nodeAddr, ctypes.ProdNetwork)

	//getting tx from node
	txHash := "9C9871AD4ADF2D525686FB0F6B18B79D6F0B09DF5147AC123C544A5C9487D4A4"
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

	//paq <- (blockDataHash | txProofRootHash | txProofLeafHash | txProofIndex | txProofTotal | txProofAunts... )
	paq := make([]byte, 0)
	paq = append(paq, blockDataHash[:]...)
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
	fmt.Println("______________________________________________________")


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
	hValidatorHash := cdcEncode(h.ValidatorsHash)
	hNexValidatorHash := cdcEncode(h.NextValidatorsHash)
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
		hValidatorHash,
		hNexValidatorHash,
		hConsensusHash,
		hAppHash,
		hLastResultsHash,
		hEvidenceHash,
		hProposerAddress}

	paqHeader := make([]byte, 0)
	for i:=0; i<len(headerArray);i++{
		paqHeader = append(paqHeader, byte(len(headerArray[i])))
		paqHeader = append(paqHeader, headerArray[i]...)
	}

	fmt.Println("____________________Encoded header____________________")
	fmt.Println(paqHeader)
	fmt.Println("______________________________________________________")

	//signatures
	//signatures from height+1
	nextBlockHeight := txBlockHeight+int64(1)
	resNextBlock, _ := client.Block(&nextBlockHeight) //get height+1 to obtain signatures of height
	s := resNextBlock.Block.LastCommit.Precommits


	paqSignatures := make([]byte, 0)
	for i:=0; i<len(s);i++{
		paqSignatures = append(paqSignatures, s[i].Signature...)
	}

	fmt.Println("______________________Signatures______________________")
	fmt.Println(paqSignatures)
	fmt.Println("______________________________________________________")

}