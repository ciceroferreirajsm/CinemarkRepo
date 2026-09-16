#!/bin/sh
set -e

echo "Creating SQS queues in LocalStack..."

awslocal sqs create-queue --queue-name film-created
awslocal sqs create-queue --queue-name film-updated
awslocal sqs create-queue --queue-name film-deleted

echo "SQS queues created."
