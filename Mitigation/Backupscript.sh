echo mounting drive
sudo mount -a
echo copying data from Ipotat to ipotat2
cp /home/victim/Desktop/Ipotat/*.* /home/victim/Desktop/ipotat2
echo setting ownership
chown root:root /home/victim/Desktop/ipotat2 -R
chmod 740 /home/victim/Desktop/ipotat2 -R
echo unmounting backup
umount /dev/sdb
