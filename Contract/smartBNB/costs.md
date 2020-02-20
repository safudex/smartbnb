# Costs

pointadd - 10 GAS  
pointmul - ~3840 GAS  
checkCompressed - ~5 GAS  
checkBytes & hash inclusion check - 119 GAS  
checkBytesV2 - 12 GAS  
sha512_modq - 173 GAS  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;sha - 117 GAS  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;sha512mod - Estimated (173 - 117 = 56 GAS)  
point_equal - ~10GAS  
Validate -  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;AreEqual -  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;VerifyBlock -  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;VerifyTx -  

As of 6/1/20

**Save data**  
General data ~6.578 GAS  
Ps_sb0 ~21.305 GAS  
Ps_sb1 ~21.305 GAS  
ss_sb0 ~3.479 GAS  
ss_sb1 ~3.479 GAS  
Qs_sb0 ~21.305 GAS  
Qs_sb1 ~21.305 GAS  
Ps_ha0 ~21.305 GAS  
Ps_ha1 ~21.305 GAS  
ss_ha0 ~3.479 GAS  
ss_ha1 ~3.479 GAS  
Qs_ha0 ~21.305 GAS  
Qs_ha1 ~21.305 GAS  

**Challenges**  
Challenge 0 (initial checks)	~12.525 GAS  
Challenge 1 (check bytes)		~12.083 GAS  
Challenge 2 (sha512)			~117.182 GAS  
Challenge 3 (mod q)				~56.757 GAS  
Challenge 4 (point equal)		~14.849 GAS  
Challenge 5 (point mul 8 steps)	~102.953 GAS  
Challenge 6 (tx proof)			~1.856 GAS  

As of 20/02/20
