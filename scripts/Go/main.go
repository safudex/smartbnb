package main

import (
	"fmt"
	"os"

	"github.com/sadufex/smartbnb/proof"
)

func main() {
	args := os.Args[1:]
	spv := proof.GetProof(args[0])
	fmt.Println(proof.Invoke(spv, args[1], args[2], args[3], args[4]))
}
