#!/bin/sh

for f in sql/*.sql; do                                       
  freeze -c freeze-config.json "$f" -o "${f%.sql}.png"
done