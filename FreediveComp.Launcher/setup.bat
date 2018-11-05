netsh http add urlacl url=http://+:3000/ user=Users
netsh advfirewall firewall add rule name="FreediveComp" dir=in action=allow protocol=TCP localport=3000
netsh advfirewall firewall add rule name="FreediveComp" dir=in action=allow protocol=UDP localport=51693
