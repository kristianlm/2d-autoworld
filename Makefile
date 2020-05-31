
autoworld: autoworld.c autoworld.fs
	$(CC) autoworld.c -o autoworld -lraylib

run: autoworld
	./autoworld
