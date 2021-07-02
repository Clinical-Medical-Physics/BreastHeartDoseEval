echo "running"
sqlcmd -U %1 -P %2 -S %3 -d variansystem -i %4 -o %5 -h-1 -s, -W -v startdate = %6 -v enddate = %7
timeout 10