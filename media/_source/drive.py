import subprocess, time, math
def x(*a): subprocess.run(['xdotool',*map(str,a)])
cur=[1599,999]
def glide(tx,ty,dur=0.8,steps=None):
    sx,sy=cur; n=steps or max(8,int(dur*40))
    for i in range(1,n+1):
        t=i/n; e=t*t*(3-2*t)
        x('mousemove',int(sx+(tx-sx)*e),int(sy+(ty-sy)*e)); time.sleep(dur/n)
    cur[0],cur[1]=tx,ty
def wiggle(cx,cy,r,dur):
    n=int(dur*40)
    for i in range(n):
        a=i/n*math.tau*2
        x('mousemove',int(cx+r*math.cos(a)),int(cy+r*math.sin(a*1.5))); time.sleep(dur/n)
    cur[0],cur[1]=cx+r,cy
def click(): x('click',1)
# board: hover across tiles
time.sleep(1.5)
for tx,ty,hold in [(380,258,1.3),(715,258,1.3),(1387,258,1.4),(715,455,1.5),(884,455,1.5),(1219,651,1.6)]:
    glide(tx,ty,0.7); time.sleep(hold)
glide(380,455,0.7); wiggle(380,455,55,3.0)   # repel
glide(884,455,0.7); time.sleep(0.6); click(); time.sleep(1.8)  # curl noise detail
glide(620,420,0.8); time.sleep(1.5)
# drag count slider
glide(1153,261,0.7); x('mousedown',1)
for px in range(1153,1380,6): x('mousemove',px,261); time.sleep(0.02)
x('mouseup',1); cur[:]= [1380,261]; time.sleep(1.2)
# drag speed slider
glide(1229,318,0.5); x('mousedown',1)
for px in range(1229,1300,4): x('mousemove',px,318); time.sleep(0.02)
x('mouseup',1); cur[:]=[1300,318]; time.sleep(1.5)
glide(252,671,0.8); click(); time.sleep(2.5)  # light surface
glide(620,420,0.8); time.sleep(1.5)
glide(197,57,0.8); click(); time.sleep(2.0)  # back
glide(1599,999,0.8); time.sleep(1.0)
