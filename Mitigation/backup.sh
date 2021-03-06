echo mounting drive
sudo mount -a
echo copying data from important to important2
cp /home/victim/Desktop/important/*.* /home/victim/Desktop/important2
echo setting ownership
chown root:root /home/victim/Desktop/important2 -R
chmod 740 /home/victim/Desktop/important2 -R
echo unmounting primary
umount /dev/sdb

