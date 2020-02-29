package main

import (
	"fmt"
	"github.com/test/test/proof"
)
func main() {
	spv := proof.GetProof("C948A2B974C93A63BDC79A13FD2CA6B93394E68F0CC8BDFDD58F39F89C256DF7")
    fmt.Println(spv.TxProof)
    fmt.Println(proof.Invoke(spv))
}
