module github.com/sadufex/smartbnb

go 1.12

require (
	github.com/binance-chain/go-sdk v1.2.0
	github.com/tendermint/tendermint v0.32.3
	google.golang.org/grpc v1.25.1 // indirect
)

replace github.com/tendermint/go-amino => github.com/binance-chain/bnc-go-amino v0.14.1-binance.1
