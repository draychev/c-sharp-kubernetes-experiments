#!/bin/bash

kubectl delete namespace $(kubectl get namespaces  --no-headers | awk '{print $1}' | grep -Ev 'kube|default') || true
