using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace sokoban
{
    static class Vars
    {
        public static readonly string LEVELS_PATH = "levels";
        public static readonly int[] RIGHT = {1,0};
        public static readonly int[] LEFT = {-1,0};
        public static readonly int[] UP = {0,-1};
        public static readonly int[] DOWN = {0,1};
        public static readonly int[] NODIR = {0,0};
        public static readonly int[] CURSOR_POS = {0,10};
        public static readonly Dictionary<Entity, char> outputLookup = new Dictionary<Entity, char> {
            {Entity.EMPTY, ' '},
            {Entity.PLAYER, '@'},
            {Entity.BOX, '$'},
            {Entity.GOAL, '.'},
            {Entity.WALL, '#'},
            {Entity.BOX_ON_GOAL, '*'},
            {Entity.PLAYER_ON_GOAL, '+'}
        };

        public static readonly Dictionary<ConsoleKey, int[]> keyToDir = new Dictionary<ConsoleKey, int[]> {
            {ConsoleKey.UpArrow, UP},
            {ConsoleKey.DownArrow, DOWN},
            {ConsoleKey.LeftArrow, LEFT},
            {ConsoleKey.RightArrow, RIGHT}
        };
    }

    [Flags]
    public enum Entity
    {
        EMPTY = 0x00,
        PLAYER = 0x01,
        BOX = 0x02,
        GOAL = 0x04,
        WALL = 0x08,
        BOX_ON_GOAL = BOX | GOAL,
        PLAYER_ON_GOAL = PLAYER | GOAL

    }


    class Program
    {
        public static string[] getLevels() {
            string[] filePaths = Directory.GetFiles(Vars.LEVELS_PATH, "*.txt");
            string[] levels = new string[filePaths.Length];
            for (int i = 0; i < filePaths.Length; i++) {
                string levelLayout = System.IO.File.ReadAllText(filePaths[i]);
                levels[i] = levelLayout;
            }
            return levels;
        }


        public static void Main(string[] args) {
            Console.CursorVisible = false;
            string[] levels = getLevels();
            int totalMovesCounter = 0;
            int currentMovesCounter = 0;
            for (int i = 0; i < levels.Length; i++) {
                Game game = new Game(levels[i]);
                game.mainLoop();
                if (game.restart) {
                    Console.Clear();
                    currentMovesCounter = 0;
                    totalMovesCounter = 0;
                    i = -1;
                    game = null;
                    continue;
                }
                currentMovesCounter = game.moveCounter;
                totalMovesCounter += currentMovesCounter;
                Console.Clear();
                Console.WriteLine(String.Format("you finished the level and moved {0} times, ", currentMovesCounter));
                if (i != levels.Length-1)
                    Console.WriteLine("press anything to go the next level.");
                else
                    Console.WriteLine(String.Format("and beat the game with a total move counter of {0}, press anything to exit.", totalMovesCounter));
                Console.ReadLine();
                Console.Clear();
            }
        }
    }



    class Game
    {
        private Board board;
        private string level;
        private int[] playerPos = new int[2];
        public int moveCounter = 0;

        public bool restart = false;

        public Game(string level) {
            this.board = new Board(level);
            this.level = level;
            this.loadLevel();
        }

        
        private void loadLevel() {
            var entityLookup = Vars.outputLookup.ToDictionary(x => x.Value, x => x.Key);
            int x = 0;
            int y = 0;
            foreach (char i in this.level) {
                if (i == '\r') continue;
                if (i == '\n') {
                    y++;
                    x = 0;
                }
                else {
                    this.board.setElement(x, y, entityLookup[i]);
                    x++;
                }
            }
        }


        public void mainLoop() {
            this.display();
            while (true) {
                playerPos = this.board.getPlayer();
                this.input();
                this.display();
                bool won = this.checkWin();
                if (won) 
                    break;
                if (this.restart)
                    break;
            }
        }


        private void input() {

            bool keepLooping = true;
            while (keepLooping) {

                while (!Console.KeyAvailable) {

                }
                var key = Console.ReadKey(true).Key;
                if (Vars.keyToDir.ContainsKey(key)) {
                    if (this.moveEntity(playerPos[0], playerPos[1], Vars.keyToDir[key]))
                        this.moveCounter++;
                    keepLooping = false;
                }
                else if (key == ConsoleKey.Enter) {
                    this.restart = true;
                    keepLooping = false;
                }
            }
        }


        private void display() {
            Console.SetCursorPosition(Vars.CURSOR_POS[0], Vars.CURSOR_POS[1]);
            for (int i = 0; i < this.board.height; i++) {
                for (int x = 0; x < this.board.width; x++) {
                    Console.Write(Vars.outputLookup[this.board.getElement(x, i)]);
                    if (x != this.board.width-1)
                        Console.Write(" ");
                }
                Console.WriteLine("");
            }
            this.displayHUD();
            Console.SetCursorPosition(Vars.CURSOR_POS[0]+this.board.width, Vars.CURSOR_POS[1]+this.board.height);
        }

        
        private void displayHUD() {
            int[] cursorPos = {0,0};
            cursorPos[1] = Math.Abs(this.board.height - Vars.CURSOR_POS[1]);
            Console.SetCursorPosition(cursorPos[0], cursorPos[1]);

            Console.WriteLine(String.Format("Moves: {0}", this.moveCounter));
            foreach (KeyValuePair<Entity, char> entry in Vars.outputLookup) {
                if (entry.Key == Entity.EMPTY) continue;
                Console.WriteLine(String.Format("{0}: {1}", entry.Key, entry.Value));
            }
            Console.WriteLine("");
        }


        private bool checkWin() {
            for (int i = 0; i < this.board.height; i++) {
                for (int x = 0; x < this.board.width; x++) {
                    if (this.board.getElement(x, i) == Entity.BOX)
                        return false;
                }
            }
            return true;
        }


        private bool moveEntity(int x, int y, int[] direction) {
            void setEntity(int x, int y, int newX, int newY, Entity entity, Entity collidingEntity, int[] direction) {
                if (entity.HasFlag(Entity.GOAL)) {
                    this.board.setElement(x, y, Entity.GOAL);
                    this.board.setElement(newX, newY, entity &= ~Entity.GOAL);
                }
                else {
                    this.board.setElement(x, y, Entity.EMPTY);
                    this.board.setElement(newX, newY, entity);
                }
                if (collidingEntity.HasFlag(Entity.GOAL)) {
                    this.board.setElement(newX, newY, (entity &= ~Entity.GOAL) | Entity.GOAL);
                }
            }


            int newX = x + direction[0];
            int newY = y + direction[1];
            Entity entity = this.board.getElement(x, y);
            Entity collidingEntity = this.board.getElement(newX, newY);
            
            if (collidingEntity.HasFlag(Entity.BOX) && entity.HasFlag(Entity.PLAYER)) {
                if (this.moveEntity(newX, newY, direction)) {
                    setEntity(x, y, newX, newY, entity, collidingEntity, direction);
                    return true;
                }
                return false;
            }
            if (collidingEntity == Entity.EMPTY) {
                setEntity(x, y, newX, newY, entity, collidingEntity, direction);
                return true;
            }
            if (collidingEntity.HasFlag(Entity.GOAL) && !collidingEntity.HasFlag(entity)) {
                setEntity(x, y, newX, newY, entity, collidingEntity, direction);
                return true;
            }
            return false;            
        }


    }


    public class Board
    {
        private Entity[,] matrix;
        public int width = 0;
        public int height = 0;


        public static int getLongestStringInArrayOfStrings(string[] arr) {
            int longest = 0;
            foreach (string i in arr) {
                if (i.Length > longest)
                    longest = i.Length;
            }
            return longest;
        }


        public Board(string level) {
            this.height = level.Split('\n').Length;
            this.width = getLongestStringInArrayOfStrings(level.Split("\n"));
            this.matrix = new Entity[this.height, this.width];
        }


        public void setElement(int x, int y, Entity newVal) {
            this.matrix[y, x] = newVal;
        }


        public Entity getElement(int x, int y) {
            return this.matrix[y, x];
        }


        public int[] getPlayer() {
            int[] playerPos = new int[2];
            for (int i = 0; i < this.height; i++) {
                for (int x = 0; x < this.width; x++) {
                    if (this.getElement(x, i).HasFlag(Entity.PLAYER)) {
                        playerPos[0] = x;
                        playerPos[1] = i;
                    }
                }
            }
            return playerPos;
        }
    }
}
