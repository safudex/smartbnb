package main

import (
	"fmt"
	"github.com/test/test/proof"
)
func main() {
	spv := proof.GetProof("87E98C672940790460055F807B0AE76C8A88826D542EB1107B6713FB102D2BC6")
    fmt.Println(proof.Invoke(spv))
}
