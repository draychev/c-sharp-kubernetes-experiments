#!make

include .env

.PHONY: clean-up-kubernetes
clean-up-kubernetes:
	./clean-up-kubernetes.sh

.PHONY: run-it
run-it:
	dotnet run --project=./

.PHONY: format
format:
	dotnet format ./kubernetes-experiments.csproj
