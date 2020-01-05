package main

import (
	"fmt"
	"github.com/test/test/proof"
)
func main() {
	spv := proof.GetProof("87E98C672940790460055F807B0AE76C8A88826D542EB1107B6713FB102D2BC6")
	fmt.Println("_______txproof_______________")
	fmt.Println(spv.TxProof)
	fmt.Println("_______header_______________")
	fmt.Println(spv.Header)
	fmt.Println("_______signatures_______________")
	fmt.Println(spv.Signatures)
	fmt.Println("_______x_______________")
	fmt.Println(spv.XSigLow)
	fmt.Println("_______y_______________")
	fmt.Println(spv.YSigLow)
	fmt.Println("_______pre_______________")
	fmt.Println(spv.PreMsg)
	fmt.Println("_______msg_______________")
	fmt.Println(spv.Msg)
	fmt.Println("_______signbytes______________")
	fmt.Println(spv.SignBytes)
	fmt.Println("______________________")
}
