package main

import (
    "encoding/hex"
    "fmt"
    "github.com/binance-chain/go-sdk/client/rpc"
    ctypes "github.com/binance-chain/go-sdk/common/types"
)

func buildProof() {

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
    paq := append(blockDataHash[:], txProofRootHash[:]...)
    paq = append(paq[:], txProofLeafHash[:]...)
    paq = append(paq[:], []byte{byte(txProofIndex)}...)//Warning: assuming txProofIndex always < byteSize
    paq = append(paq[:], []byte{byte(txProofTotal)}...)//Warning: assuming txProofTotal always < byteSize
    for i := 0; i < len(txProofAunts); i++ {
        paq = append(paq[:], txProofAunts[i][:]...)
    }

    fmt.Println("________________Send to smart contract________________")
    fmt.Println(paq)
    fmt.Println("______________________________________________________")


    //block connexion
    //actual block id
    //blockId := resBlock.BlockMeta.BlockID.Hash
    //fmt.Println("blockId: ", blockId)

    //last block id from height+1
    //nextBlockHeight := txBlockHeight+1
    //resNextBlock, _ := client.Block(&nextBlockHeight) //get block+1 2 obtain connexion + signatures
    //nextBlockLastId := resNextBlock.Block.Header.LastBlockID.Hash
    //fmt.Println("nextBlockLastId", nextBlockLastId)

}
