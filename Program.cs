using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace EngineOfChess {

    internal class Program {

        static string positionFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 ";
        static string positionFen2 = "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1";

        private static readonly Random rng = new Random();
        public static readonly int[] BitTable = new int[64] {
    63, 30, 3, 32, 25, 41, 22, 33, 15, 50, 42, 13, 11, 53, 19, 34,
    61, 29, 2, 51, 21, 43, 45, 10, 18, 47, 1, 54, 9, 57, 0, 35,
    62, 31, 40, 4, 49, 5, 52, 26, 60, 6, 23, 44, 46, 27, 56, 16,
    7, 39, 48, 24, 59, 14, 12, 55, 38, 28, 58, 20, 37, 17, 36, 8
};
        unsafe
        static void Main(string[] args) {

            S_BOARD board = new();
            InitSq120To64();
            InitBitMasks();
            InitHashKeys();

            ParseFen(positionFen, ref board);
            PrintBoard(in board);
            ParseFen(positionFen2, ref board);
            PrintBoard(in board);

            ulong playBitBoard = 0UL;
            ulong x = Rand64();
            var y = Convert.ToString((long)x, 2).PadLeft(64, '0');
        }

        static void InitSq120To64() {
            int index = 0;
            int file = (int)File.FILE_A;
            int rank = (int)Rank.RANK_1;
            int Sq = (int)BoardSquare.A1;
            int Sq64 = 0;

            for (index = 0; index < Global.BRD_SQ_NUM; index++) {
                Global.Sq120ToSq64[index] = 65;
            }

            for (index = 0; index < 64; index++) {
                Global.Sq64ToSq120[index] = 120;
            }

            for (rank = (int)Rank.RANK_1; rank <= (int)Rank.RANK_8; rank++) {

                for (file = (int)File.FILE_A; file <= (int)File.FILE_H; file++) {

                    Sq = Global.FR2SQ(file, rank);

                    Global.Sq64ToSq120[Sq64] = Sq;
                    Global.Sq120ToSq64[Sq] = Sq64;

                    Sq64++;
                }
            }
        }

        static void InitBitMasks() {
            int index = 0;

            for (index = 0; index < 64; index++) {
                Global.SetMask[index] = 0UL;
                Global.ClearMask[index] = 0UL;
            }

            for (index = 0; index < 64; index++) {
                Global.SetMask[index] = (1UL << index);
                Global.ClearMask[index] = ~Global.SetMask[index];
            }
        }

        static void InitHashKeys() {
            int index = 0;
            int index2 = 0;

            for (index = 0; index < 13; index++) {
                for (index2 = 0; index2 < 120; index2++) {
                    Global.PieceKeys[index][index2] = Rand64();
                }
            }
            Global.SideKey = Rand64();

            for (index = 0; index < 16; index++) {
                Global.CastleKeys[index] = Rand64();
            }
        }

        unsafe public static ulong GeneratePosKey(in S_BOARD pos) {
            int sq = 0;
            ulong finalKey = 0;
            int piece = (int)Pieces.EMPTY;

            for (sq = 0; sq < Global.BRD_SQ_NUM; sq++) {
                piece = pos.pieces[sq];
                if (piece != (int)BoardSquare.NO_SQ && piece != (int)Pieces.EMPTY) {
                    ASSERT(piece >= (int)Pieces.wP && piece <= (int)Pieces.bK);

                    finalKey = Global.PieceKeys[piece][sq];
                }
            }

            if (pos.side == (int)Color.WHITE) {
                finalKey ^= Global.SideKey;
            }

            if (pos.enPas != (int)BoardSquare.NO_SQ) {
                ASSERT(pos.enPas >= 0 && pos.enPas < Global.BRD_SQ_NUM);
                finalKey ^= Global.PieceKeys[(int)Pieces.EMPTY][pos.enPas];

            }

            ASSERT(pos.castlePerm >= 0 && pos.castlePerm <= 15);
            finalKey ^= Global.CastleKeys[pos.castlePerm];

            return finalKey;
        }
        public static int ParseFen(string fen, ref S_BOARD pos) {
            if (fen == null) {
                Console.WriteLine("FEN nula!");
                return -1;
            }

            int rank = (int)Rank.RANK_8;
            int file = (int)File.FILE_A;
            int piece = 0;
            int count = 0;
            int fenPos = 0;

            ResetBoard(pos);

            // ------------------------------
            // PARSE DAS PEÇAS 8 RANKS
            // ------------------------------
            while (rank >= (int)Rank.RANK_1 && fenPos < fen.Length) {
                char c = fen[fenPos];
                count = 1;

                if (char.IsDigit(c)) // casas vazias
                {
                    piece = (int)Pieces.EMPTY;
                    count = c - '0';       // CORRIGIDO
                }
                else {
                    switch (c) {
                        case 'p': piece = (int)Pieces.bP; break;
                        case 'r': piece = (int)Pieces.bR; break;
                        case 'n': piece = (int)Pieces.bN; break;
                        case 'b': piece = (int)Pieces.bB; break;
                        case 'k': piece = (int)Pieces.bK; break;
                        case 'q': piece = (int)Pieces.bQ; break;

                        case 'P': piece = (int)Pieces.wP; break;
                        case 'R': piece = (int)Pieces.wR; break;
                        case 'N': piece = (int)Pieces.wN; break;
                        case 'B': piece = (int)Pieces.wB; break;
                        case 'K': piece = (int)Pieces.wK; break;
                        case 'Q': piece = (int)Pieces.wQ; break;

                        case '/':
                            rank--;
                            file = (int)File.FILE_A;
                            fenPos++;
                            continue;

                        case ' ':
                            rank--;
                            file = (int)File.FILE_A;
                            fenPos++;
                            continue;

                        default:
                            Console.WriteLine($"FEN error: caractere inválido '{c}'");
                            return -1;
                    }
                }

                for (int i = 0; i < count; i++) {
                    int sq64 = rank * 8 + file;
                    int sq120 = Global.Sq64ToSq120[sq64];

                    if (piece != (int)Pieces.EMPTY)
                        pos.pieces[sq120] = piece;

                    file++;
                }

                fenPos++;
            }

            // ------------------------------
            // LADO A JOGAR
            // ------------------------------
            if (fen[fenPos] == 'w' || fen[fenPos] == 'b') {
                pos.side = fen[fenPos] == 'w' ? (int)Color.WHITE : (int)Color.BLACK;
                fenPos += 2; // pular letra + espaço
            }

            // ------------------------------
            // PERMISSÕES DE ROQUE
            // ------------------------------
            pos.castlePerm = 0;

            while (fen[fenPos] != ' ') {
                switch (fen[fenPos]) {
                    case 'K': pos.castlePerm |= (int)Casteling.WKCA; break;
                    case 'Q': pos.castlePerm |= (int)Casteling.WQCA; break;
                    case 'k': pos.castlePerm |= (int)Casteling.BKCA; break;
                    case 'q': pos.castlePerm |= (int)Casteling.BQCA; break;
                    case '-': break;
                    default:
                        Console.WriteLine("FEN error na parte de roques");
                        return -1;
                }

                fenPos++;
            }

            fenPos++; // pular espaço

            // ------------------------------
            // CASA EN PASSANT
            // ------------------------------
            if (fen[fenPos] != '-') {
                int fileEP = fen[fenPos] - 'a';
                int rankEP = fen[fenPos + 1] - '1';

                if (fileEP >= 0 && fileEP <= 7 && rankEP >= 0 && rankEP <= 7)
                    pos.enPas = Global.FR2SQ(fileEP, rankEP);
                else
                    pos.enPas = (int)BoardSquare.NO_SQ;

                fenPos += 2;
            }
            else {
                pos.enPas = (int)BoardSquare.NO_SQ;
                fenPos++;
            }

            // ------------------------------
            // CHAVE ZOBRIST
            // ------------------------------
            pos.posKey = GeneratePosKey(in pos);

            return 0;
        }

        unsafe public static void ResetBoard(S_BOARD pos) {

            for (int index = 0; index < Global.BRD_SQ_NUM; index++) {
                pos.pieces[index] = (int)BoardSquare.NO_SQ;
            }

            for (int index = 0; index < 64; index++) {
                pos.pieces[Global.Sq64ToSq120[index]] = (int)Pieces.EMPTY;
            }

            for (int index = 0; index < 3; index++) {
                pos.bigPce[index] = 0;
                pos.majPce[index] = 0;
                pos.minPce[index] = 0;
                pos.pawns[index] = 0UL;
            }

            for (int index = 0; index < 13; index++) {
                pos.pceNum[index] = 0;
            }

            pos.kingSq[(int)Color.WHITE] = pos.kingSq[(int)Color.BLACK] = (int)BoardSquare.NO_SQ;

            pos.side = (int)Color.BOTH;
            pos.enPas = (int)BoardSquare.NO_SQ;
            pos.fiftyMove = 0;

            pos.ply = 0;
            pos.hisPly = 0;

            pos.castlePerm = 0;

            pos.posKey = 0UL;
        }
        public static void PrintBoard(in S_BOARD pos) {
            int sq, file, rank, piece;

            Console.WriteLine();
            Console.WriteLine("Game board");

            for (rank = (int)Rank.RANK_8; rank >= (int)Rank.RANK_1; rank--) {
                Console.Write(rank + 1 + " ");
                for (file = (int)File.FILE_A; file <= (int)File.FILE_H; file++) {
                    sq = Global.FR2SQ(file, rank);
                    piece = pos.pieces[sq];
                    Console.Write(Global.PceChar[piece] + " ");
                }
                Console.WriteLine();
            }
            Console.WriteLine("  a b c d e f g h");
            Console.WriteLine();
            Console.WriteLine("side: " + Global.SideChar[pos.side]);
            Console.WriteLine("enPas: " + pos.enPas);
            Console.Write("castle: ");
            Console.Write((pos.castlePerm & (int)Casteling.WKCA) != 0 ? 'K' : '-');
            Console.Write((pos.castlePerm & (int)Casteling.WQCA) != 0 ? 'Q' : '-');
            Console.Write((pos.castlePerm & (int)Casteling.BKCA) != 0 ? 'k' : '-');
            Console.Write((pos.castlePerm & (int)Casteling.BQCA) != 0 ? 'q' : '-');
            Console.WriteLine();
            Console.WriteLine($"posKey: {pos.posKey:X}");
        }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBit(ref ulong bb, int sq) {
            bb |= Global.SetMask[sq];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ClearBit(ref ulong bb, int sq) {
            bb &= Global.ClearMask[sq];
        }
        public static ulong Rand64() {
            Random r = new Random();

            ulong a = (ulong)r.Next(0, 1 << 15);        // bits 0-14
            ulong b = (ulong)r.Next(0, 1 << 15) << 15;  // bits 15-29
            ulong c = (ulong)r.Next(0, 1 << 15) << 30;  // bits 30-44
            ulong d = (ulong)r.Next(0, 1 << 15) << 45;  // bits 45-59
            ulong e = ((ulong)r.Next(0, 16)) << 60;     // bits 60-63 (0xF = 15 = 4 bits)

            return a | b | c | d | e;
        }
        unsafe
        static int PopBit(ulong* bb) {
            ulong b = *bb ^ (*bb - 1);
            int fold = (int)((b & 0xffffffff) ^ (b >> 32));
            *bb &= (*bb - 1);
            return BitTable[(fold * 0x783a9b23) >> 26 & 63];
        }
        static int CountBits(ulong b) {
            int r = 0;

            for (r = 0; b != 0; r++, b &= b - 1) {

            }
            return r;
        }

        static void PrintBitBoard(ulong bb) {

            ulong shiftMe = 1UL;

            int rank = 0;
            int file = 0;
            int sq = 0;
            int sq64 = 0;

            Console.WriteLine();

            for (rank = (int)Rank.RANK_8; rank >= (int)Rank.RANK_1; rank--) {
                for (file = (int)File.FILE_A; file <= (int)File.FILE_H; file++) {

                    sq = Global.FR2SQ(file, rank);
                    sq64 = Global.Sq120ToSq64[sq];

                    if (((shiftMe << sq64) & bb) != 0) {
                        Console.Write("X");
                    }
                    else {
                        Console.Write("-");
                    }
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }

        static void ASSERT(bool condition) {
            if (!condition)
                throw new Exception("ASSERT FAILED");
        }

    }

    enum Pieces { EMPTY, wP, wN, wB, wR, wQ, wK, bP, bN, bB, bR, bQ, bK };
    enum File { FILE_A, FILE_B, FILE_C, FILE_D, FILE_E, FILE_F, FILE_G, FILE_H, FILE_NONE };
    enum Rank { RANK_1, RANK_2, RANK_3, RANK_4, RANK_5, RANK_6, RANK_7, RANK_8, RANK_NONE };
    enum Color { WHITE, BLACK, BOTH };
    enum BoardSquare {
        A1 = 21, B1, C1, D1, E1, F1, G1, H1,
        A2 = 31, B2, C2, D2, E2, F2, G2, H2,
        A3 = 41, B3, C3, D3, E3, F3, G3, H3,
        A4 = 51, B4, C4, D4, E4, F4, G4, H4,
        A5 = 61, B5, C5, D5, E5, F5, G5, H5,
        A6 = 71, B6, C6, D6, E6, F6, G6, H6,
        A7 = 81, B7, C7, D7, E7, F7, G7, H7,
        A8 = 91, B8, C8, D8, E8, F8, G8, H8, NO_SQ, OFFBOARD
    };
    enum Bools { FALSE, TRUE };
    enum Casteling { WKCA = 1, WQCA = 2, BKCA = 4, BQCA = 8 };

    public struct S_BOARD {

        public int[] pieces = new int[Global.BRD_SQ_NUM];
        public ulong[] pawns = new ulong[3]; // 01000000 00000000 00000000 00000000 00000000

        public int[] kingSq = new int[2];

        public int side;
        public int enPas;
        public int fiftyMove;

        public int ply;
        public int hisPly;

        public int castlePerm;

        public ulong posKey;

        public int[] pceNum = new int[13];
        public int[] bigPce = new int[3];
        public int[] majPce = new int[3];
        public int[] minPce = new int[3];

        S_UNDO[] history = new S_UNDO[Global.MAXGAMEMOVE];

        public int[][] pList = new int[13][];

        public S_BOARD() {

            for (int i = 0; i < 13; i++) {
                pList[i] = new int[10];
            }
            for (int i = 0; i < 13; i++) {
                Global.PieceKeys[i] = new ulong[120];
            }
        }
    }
    struct S_UNDO {

        int move;
        int castlePerm;
        int enPas;
        int fiftyMove;
        ulong posKey;
    }

    static class Global {

        public static int BRD_SQ_NUM = 120;
        public static int MAXGAMEMOVE = 2048;

        public static int[] Sq120ToSq64 = new int[BRD_SQ_NUM];
        public static int[] Sq64ToSq120 = new int[64];

        public static ulong[] SetMask = new ulong[64];
        public static ulong[] ClearMask = new ulong[64];

        public static ulong[][] PieceKeys = new ulong[13][];
        public static ulong SideKey;
        public static ulong[] CastleKeys = new ulong[16];

        public static string PceChar = ",PNBRQKpnbrqk";
        public static string SideChar = "wb-";
        public static string RankChar = "12345678";
        public static string FileChar = "abcdefgh";

        public static int FR2SQ(int f, int r) {

            return (21 + f) + (r * 10);
        }
    }
}
