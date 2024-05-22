#/bin/bash

git remote -v

git pull --set-upstream homecloud master
git pull --set-upstream origin master

git push -u homecloud master
git push -u origin master