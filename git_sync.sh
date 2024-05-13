#/bin/bash

git remote -v

git push -u homecloud master
git pull --set-upstream homecloud master

git push -u origin master
git pull --set-upstream origin master