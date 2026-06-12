graph TD

%% ROOT
TTS1[TTS 1: Pre-Industrial]

%% TTS 2
TTS2[TTS 2: Industrial Age]

%% TTS 3
TTS3[TTS 3: Early Electronics]

%% TTS 4
TTS4[TTS 4: Information Age]

%% TTS 5
TTS5[TTS 5: Early AI / Quantum Age]

%% TTS 6
TTS6[TTS 6: Bio / Nano Age]

%% TTS 7
TTS7[TTS 7: Temporal Age]

%% TTS 8+
TTS8[TTS 8+: Post-Singularity]

%% MAIN PROGRESSION PATH
TTS1 --> TTS2 --> TTS3 --> TTS4 --> TTS5 --> TTS6 --> TTS7 --> TTS8

%% BRANCHES FROM TTS1
TTS1 --> AGR[Agriculture]
TTS1 --> CRAFT[Craftsmanship]
TTS1 --> GOV[Early Governance]

%% BRANCHES FROM TTS2
TTS2 --> MECH[Mechanization]
TTS2 --> PROD[Mass Production]
TTS2 --> LOG[Logistics Systems]

%% TTS2 UNLOCKS
MECH --> TTS3
PROD --> TTS3
LOG --> TTS3

%% BRANCHES TTS3
TTS3 --> COMM[Communication Systems]
TTS3 --> ELEC[Electrical Infrastructure]
TTS3 --> AVI[Aviation Systems]

COMM --> TTS4
ELEC --> TTS4
AVI --> TTS4

%% TTS4 BRANCHES
TTS4 --> DIG[Digital Economy]
TTS4 --> NET[Global Networks]
TTS4 --> CYB[Cyber Systems]

DIG --> TTS5
NET --> TTS5
CYB --> TTS5

%% TTS5 BRANCHES
TTS5 --> AI[Artificial Intelligence]
TTS5 --> QUANT[Quantum Computing]
TTS5 --> AUTO[Automation Systems]

AI --> TTS6
QUANT --> TTS6
AUTO --> TTS6

%% TTS6 BRANCHES
TTS6 --> GEN[Genetic Engineering]
TTS6 --> NANO[Nanotechnology]
TTS6 --> HUM[Human Enhancement]

GEN --> TTS7
NANO --> TTS7
HUM --> TTS7

%% TTS7 BRANCHES
TTS7 --> TIME[Timeline Engineering]
TTS7 --> DIV[Timeline Divergence]
TTS7 --> PAR[Paradox Systems]

TIME --> TTS8
DIV --> TTS8
PAR --> TTS8

%% TTS8+ BRANCHES
TTS8 --> SUP[Superintelligence Governance]
TTS8 --> REAL[Reality Simulation Control]
TTS8 --> PHYS[Physics Modification]
