package main

import (
	"os"

	"github.com/test/test/proof"
)

func main() {
	args := os.Args[1:]
	spv := proof.GetProof(args[0])
	proof.Invoke(spv, args[1], args[2], args[3])
}
