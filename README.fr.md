# Forza Telemetry Splitter

[English](README.md) · [日本語](README.ja.md) · **Français** · [Deutsch](README.de.md) · [Español](README.es.md) · [简体中文](README.zh-Hans.md)

Envoyez la télémétrie de Forza à plusieurs outils à la fois.

La télémétrie « Data Out » de Forza Horizon 6 ne peut être envoyée qu'à une seule adresse IP et un seul
port. Cela oblige à choisir : alimenter [VirtualTCU](https://github.com/Forza-Love/fh6-virtual_tcu)
(passage de vitesses automatique), un outil de réglage, ou un tableau de bord — mais pas tous en même
temps.

Forza Telemetry Splitter s'intercale entre les deux. Il reçoit la télémétrie de Forza sur son propre
port et renvoie chaque paquet, inchangé, à autant d'outils locaux que vous le souhaitez. La latence
ajoutée est inférieure à la milliseconde et les données ne sont pas modifiées : chaque outil se comporte
exactement comme s'il communiquait directement avec Forza.

Sans affiliation ni approbation de Turn 10, Playground Games ou Microsoft. « Forza » est une marque de
Microsoft.

## Fonctionnalités

| Fonctionnalité | Description |
|----------------|-------------|
| Répartition | Répartit la télémétrie de Forza vers un nombre illimité de destinations, paquets inchangés. |
| Multi-jeux | Compatible Forza Horizon 4/5/6 et Forza Motorsport (7, 2023). Le jeu est détecté automatiquement. |
| Overlay de statut | Une petite pastille en haut à droite affiche « Connecté / Aucune donnée » ainsi que le rapport et la vitesse en direct. |
| Multilingue | Anglais, japonais, français, allemand, espagnol. Sélectionné automatiquement selon la langue de Windows. |
| Application en barre d'état | Tourne discrètement dans la zone de notification, comme VirtualTCU. |
| Aucun administrateur requis | UDP local uniquement : pas d'invite UAC. |

## Installation

Recommandé — le programme d'installation :

1. Téléchargez `ForzaTelemetrySplitterInstaller.exe` depuis la page [Releases](../../releases).
2. Clic droit → Propriétés → cochez « Débloquer » en bas de l'onglet Général → OK. Cela évite l'écran
   « Windows a protégé votre ordinateur ».
3. Lancez-le. Installation par utilisateur, donc aucune invite d'administrateur. Propose un raccourci
   sur le bureau et une option « Démarrer automatiquement avec Windows ».
4. Il démarre dans la barre d'état une fois terminé.

Avancé / sans installation : téléchargez plutôt `ftsPortable.exe` et lancez-le directement. Le
programme d'installation ci-dessus est recommandé pour la plupart des utilisateurs.

## Configuration dans le jeu

1. Ouvrez l'application depuis la barre d'état. Elle écoute sur le port **44405** et est déjà réglée
   pour transférer vers VirtualTCU (son port habituel 5555).
2. Dans votre jeu Forza, ouvrez Data Out (sous Horizon : Paramètres → HUD et gameplay → Data Out) :
   - Data Out : activé
   - Adresse IP : `127.0.0.1`
   - Port : **`44405`**
   - Format de paquet : Car Dash (Horizon) ou Dash (Motorsport)
3. Laissez vos autres outils tels quels — le répartiteur transfère vers chacun sur son port habituel.
   Pour ajouter un outil, cliquez sur « Ajouter » dans l'application.
4. Roulez : la pastille en haut à droite passe au vert et tous les outils activés reçoivent les données.

## Plus d'informations (en anglais)

- [Compilation depuis les sources](docs/BUILDING.md)
- [Avertissement Windows SmartScreen](docs/SMARTSCREEN.md)
- [Signaler un bug](docs/REPORTING-BUGS.md)
- [Contribuer](CONTRIBUTING.md)
- [Licence (MIT)](LICENSE)

## Testé sur

Windows 10 et 11. Forza Horizon 4/5/6 et Forza Motorsport (7, 2023) — détecté automatiquement.
