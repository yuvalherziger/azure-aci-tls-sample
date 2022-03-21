#!make

DOCKER_BIN := docker
IMAGE_NAME := aci-demo
IMAGE_TAG := latest

.PHONY: build
build:
	$(DOCKER_BIN) build -t $(IMAGE_NAME):$(IMAGE_TAG) .
