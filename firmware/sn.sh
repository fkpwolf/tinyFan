uuid=`uuidgen`
sn=${uuid:0:5}F
echo "device sn is: $sn"

sn1=`printf "%d" "'${sn:0:1}"`
sn2=`printf "%d" "'${sn:1:1}"`
sn3=`printf "%d" "'${sn:2:1}"`
sn4=`printf "%d" "'${sn:3:1}"`
sn5=`printf "%d" "'${sn:4:1}"`
sn6=`printf "%d" "'${sn:5:1}"`

`../commandline/hidtool write 125 $sn1 $sn2 $sn3 $sn4 $sn5 $sn6`
