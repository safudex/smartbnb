package proof

import (
	"encoding/binary"
	"encoding/hex"
	"io"
	"os"
	"os/exec"
	"strconv"
	"strings"
	"time"

	"github.com/binance-chain/go-sdk/client/rpc"
	ctypes "github.com/binance-chain/go-sdk/common/types"
	"github.com/binance-chain/go-sdk/types"
	cmn "github.com/tendermint/tendermint/libs/common"
	v "github.com/tendermint/tendermint/types"
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
	timestamp      time.Time //string
	hashBlock      []byte
	partsHash      []byte //string//TODO: check if ok ARRAY
	partsTotal     int
	validatorAddr  []byte
	validatorIndex int
	height         int64
	round          int
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
		ValidatorIndex:   vd.validatorIndex,
	}
}

func VoteSignableHexBytes(vd voteData) string {
	vote := createPrecommit(vd)
	signBytes := vote.SignBytes("Binance-Chain-Tigris")
	timeStart := 92
	if vd.round > 0 {
		timeStart = 102
	}
	signBytes = append([]byte{byte(vd.validatorIndex)}, signBytes...)
	signBytes = append([]byte{byte(timeStart + 5)}, signBytes...)
	signBytes = append([]byte{byte(timeStart)}, signBytes...)
	signBytes = append([]byte{byte(vd.round)}, signBytes...)
	return hex.EncodeToString(signBytes)
}

func Decompress(s string) (string, string, string) {
	out, err := exec.Command("python", "helper.py", "1", s).Output()
	if err != nil {
		panic(err)
	}
	r := strings.Split(string(out), "\n")
	return r[0], r[1], r[2]
}

func GetSbHa(msg string, sig string, pubk string) (string, string, string, string, string) {
	out, err := exec.Command("python", "helper.py", "3", msg, sig, pubk).Output()
	if err != nil {
		panic(err)
	}
	r := strings.Split(string(out), "\n")
	return r[0], r[1], r[2], r[3], r[4]
}

func GetPreprocessedMsg(msg string) string {
	out, err := exec.Command("python", "helper.py", "2", msg).Output()
	if err != nil {
		panic(err)
	}
	r := strings.Split(string(out), "\n")
	return r[0]
}

func GetPointMulSteps(isHA string, sigint string, its string, pubk string) string {
	out, err := exec.Command("python", "helper.py", "4", isHA, sigint, its, pubk).Output()
	if err != nil {
		panic(err)
	}
	r := strings.Split(string(out), "\n")
	joined := ""
	s := ""
	P := ""
	Q := ""
	it, _ := strconv.Atoi(its)
	for i := 0; i < 256/it; i++ {
		s += r[i*3]
		P += r[i*3+1]
		Q += r[i*3+2]
		if i < (256/it)-1 {
			s += ","
			P += ","
			Q += ","
		}
	}
	joined += s + "," + Q + "," + P
	return joined
}

func WriteStringToFile(filepath, s string) error {
	fo, err := os.Create(filepath)
	if err != nil {
		return err
	}
	defer fo.Close()

	_, err = io.Copy(fo, strings.NewReader(s))
	if err != nil {
		return err
	}

	return nil
}

//r := strings.Split(string(out), "\n")
func Invoke(spv SPV, privk string, nodeUrl string, pcid string, script_hash string) string {
	WriteStringToFile("pointmulsteps", spv.MulStepsSB+"||"+spv.MulStepsHA)
	out, err := exec.Command("node", "invoke/invoke.js",
		privk,
		nodeUrl,
		pcid,
		script_hash,
		spv.Signatures,
		spv.XSigLow,
		spv.YSigLow,
		spv.SignBytes,
		spv.PresMsg,
		spv.PresHash,
		spv.PresHashMod,
		spv.TxProof,
		spv.Header,
		spv.TxBytes).CombinedOutput()
	if err != nil {
		panic(err)
	}
	return string(out)
}

type SPV struct {
	TxProof     string
	Header      string
	Signatures  string
	XSigLow     string
	YSigLow     string
	PresMsg     string
	SignBytes   string
	TxId        string
	PresHash    string
	PresHashMod string
	SB          string
	HA          string
	MulStepsSB  string
	MulStepsHA  string
	TxBytes     string
}

func GetProof(txHash string) SPV {
	spv := SPV{}
	spv.TxId = txHash
	//init rpc client
	nodeAddr := "http://dataseed2.binance.org:80"
	client := rpc.NewRPCClient(nodeAddr, ctypes.ProdNetwork)
	//getting tx from node
	bytesTxHash, _ := hex.DecodeString(txHash)
	restx, _ := client.Tx(bytesTxHash, true)

	txbytes := restx.Proof.Data
	start, l := getOutputStart(txbytes)
	paqtx := make([]byte, 0)
	paqtx = append(paqtx, []byte{byte(start)}...) //Warning: assuming txProofIndex always < byteSize
	paqtx = append(paqtx, []byte{byte(l)}...)     //Warning: assuming txProofIndex always < byteSize
	paqtx = append(paqtx, txbytes...)
	strtx := ""
	for i := 0; i < len(paqtx); i++ {
		strtx += strconv.Itoa(int(paqtx[i]))
		if i < len(paqtx)-1 {
			strtx += ","
		}
	}

	spv.TxBytes = strtx //hex.EncodeToString(paqtx)

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
	//	txProofRootHash := restx.Proof.RootHash

	//getting block info to get data hash
	resBlock, _ := client.Block(&txBlockHeight)
	//paq <- (txProofRootHash | txProofLeafHash | txProofIndex | txProofTotal | txProofAunts... )
	paq := make([]byte, 0)
	//paq = append(paq, txProofRootHash[:]...)
	paq = append(paq, txProofLeafHash[:]...)
	paq = append(paq, []byte{byte(txProofIndex)}...) //Warning: assuming txProofIndex always < byteSize
	paq = append(paq, []byte{byte(txProofTotal)}...) //Warning: assuming txProofTotal always < byteSize
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

	var headerArray = [16][]byte{
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
	for i := 0; i < len(headerArray); i++ {
		paqHeader = append(paqHeader, byte(len(headerArray[i])))
		paqHeader = append(paqHeader, headerArray[i]...)
	}

	spv.Header = hex.EncodeToString(paqHeader)

	//signatures
	//signatures from height+1
	nextBlockHeight := txBlockHeight + int64(1)
	resNextBlock, _ := client.Block(&nextBlockHeight) //get height+1 to obtain signatures of height
	s := resNextBlock.Block.LastCommit.Precommits
	s = s[:8]

	//paqSignatures <- (signature0 | signature1 | signature2 | ... | signature10 )
	paqSignatures := make([]byte, 0)
	strSignatures := ""
	for i := 0; i < len(s); i++ {
		paqSignatures = append(paqSignatures, s[i].Signature...)
		strSignatures += hex.EncodeToString(s[i].Signature)
		if i < len(s)-1 {
			strSignatures += ","
		}
	}

	spv.Signatures = strSignatures

	strX := ""
	strY := ""
	sig_high := ""
	for i := 0; i < len(s); i++ {
		sig_high = hex.EncodeToString(s[i].Signature[:32])
		s_x, s_y, _ := Decompress(sig_high)
		strX += s_x
		strY += s_y
		if i < len(s)-1 {
			strX += ","
			strY += ","
		}
	}

	spv.XSigLow = strX
	spv.YSigLow = strY

	pubks := []string{
		"d3769d8a1f78b4c17a965f7a30d4181fabbd1f969f46d3c8e83b5ad4845421d8",
		"2ba4e81542f437b7ae1f8a35ddb233c789a8dc22734377d9b6d63af1ca403b61",
		"df8da8c5abfdb38595391308bb71e5a1e0aabdc1d0cf38315d50d6be939b2606",
		"b6619edca4143484800281d698b70c935e9152ad57b31d85c05f2f79f64b39f3",
		"9446d14ad86c8d2d74780b0847110001a1c2e252eedfea4753ebbbfce3a22f52",
		"0353c639f80cc8015944436dab1032245d44f912edc31ef668ff9f4a45cd0599",
		"e81d3797e0544c3a718e1f05f0fb782212e248e784c1a851be87e77ae0db230e",
		"5e3fcda30bd19d45c4b73688da35e7da1fce7c6859b2c1f20ed5202d24144e3e",
		"b06a59a2d75bf5d014fce7c999b5e71e7a960870f725847d4ba3235baeaa08ef",
		"0c910e2fe650e4e01406b3310b489fb60a84bc3ff5c5bee3a56d5898b6a8af32",
		"71f2d7b8ec1c8b99a653429b0118cd201f794f409d0fea4d65b1b662f2b00063"}

	signBytes := ""
	msg := ""
	pres := ""
	presHash := ""
	presHashMod := ""
	sig := ""
	sB := ""
	hA := ""
	stepsSB := ""
	stepsHA := ""
	for i := 0; i < len(s); i++ {
		vote := voteData{
			timestamp:      s[i].Timestamp,
			hashBlock:      s[i].BlockID.Hash,
			partsHash:      s[i].BlockID.PartsHeader.Hash,
			partsTotal:     s[i].BlockID.PartsHeader.Total,
			validatorAddr:  s[i].ValidatorAddress,
			validatorIndex: s[i].ValidatorIndex,
			height:         int64(s[i].Height),
			round:          s[i].Round,
		}

		sBytes := VoteSignableHexBytes(vote)
		signBytes += sBytes
		if i < len(s)-1 {
			signBytes += ","
		}

		sig = hex.EncodeToString(s[i].Signature)
		msg = hex.EncodeToString(s[i].Signature[:32])
		msg = msg + pubks[i] + sBytes[8:]
		pre := GetPreprocessedMsg(msg)
		preHash, preHashMod, s_int, sb, ha := GetSbHa(msg, sig, pubks[i])
		pres += pre
		presHash += preHash
		presHashMod += preHashMod
		sB += sb
		hA += ha

		stepSB := GetPointMulSteps("false", s_int, "8", "")
		stepsSB += stepSB

		stepHA := GetPointMulSteps("true", preHashMod, "8", pubks[i])
		stepsHA += stepHA

		if i < len(s)-1 {
			pres += " "
			presHash += " "
			presHashMod += ","
			sB += " "
			hA += " "
			stepsSB += " "
			stepsHA += " "
		}
	}
	spv.MulStepsSB = stepsSB
	spv.MulStepsHA = stepsHA
	spv.SignBytes = signBytes

	spv.PresMsg = pres
	spv.PresHash = presHash
	spv.PresHashMod = presHashMod
	spv.SB = sB
	spv.HA = hA
	return spv
}

func DecodeUvarint(bz []byte) (u uint64, n int) {
	u, n = binary.Uvarint(bz)
	if n == 0 {
		// buf too small
		return
	} else if n < 0 {
		// value larger than 64 bits (overflow)
		// and -n is the number of bytes read
		n = -n
		return
	}
	return
}

func getOutputStart(bz []byte) (start int, length int) {
	_, n := binary.Uvarint(bz)
	bz = bz[n:]
	start += n

	bz = bz[4:]
	start += 4

	var value64 = uint64(0)
	value64, n = DecodeUvarint(bz)
	bz = bz[n:]
	start += n

	value64, n = DecodeUvarint(bz)
	bz = bz[n:]
	start += n

	//10 78 42 entra dentro de slice no byte
	//slide field number and type
	value64, n = DecodeUvarint(bz)
	bz = bz[n:]
	start += n

	var count, _n = uint64(0), int(0)
	//buf, _n, err = DecodeByteSlice(bz)
	count, _n = DecodeUvarint(bz)
	bz = bz[_n:]
	start += _n
	//	bz = bz[:count]

	//decode fieldnumber and type
	value64, n = DecodeUvarint(bz)
	bz = bz[n:]
	start += n

	//35 10 20
	//buf, _n, err = DecodeByteSlice(bz)
	count, _n = DecodeUvarint(bz)
	bz = bz[_n:]
	start += n
	bz = bz[count:]
	start += int(count)

	//	if slide(&bz, nil, _n) && err != nil {
	//en buf hay guardado input

	//entonces
	bz = bz[_n:] //aqui bz empieza por 18 el resultado
	start += _n

	//			tenemos output(18 mas output + cosas)
	//decode fieldnumber and type
	value64, n = DecodeUvarint(bz)
	bz = bz[:value64+1]

	//decode fieldnumber and type
	/*value64, n = DecodeUvarint(bz)
	bz = bz[n:]
	start += n*/
	length = len(bz)
	return
}
