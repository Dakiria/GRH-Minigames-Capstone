using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//The balloon pop game manager extends from the base game manager class.
public class GRHBalloonMG_GameManager : GRH_GameManager
{
    //Initial setup - Joseph
    enum BalloonPopGameStates { Introduction, PlayerTurn, AI1Turn, AI2Turn, AI3Turn, GameEnd };

    BalloonPopGameStates currentGameState;

    //The array for whether a player is still in the game or not. Locations are as follows:
    //0 - Player / 1 - AI 1 / 2 - AI 2 / 3 - AI 3
    bool[] activePlayers = new bool[4];

    int maxBalloonPumps, currentBalloonPumps;

    GRHBalloonMG_AIController aiController; //Controls th AI's decisions [Added by Bryce]

    [SerializeField] Text balloonPumpsLeftText; // The Display on the balloon of how many pumps are left [Added by Bryce]

    GRHBalloonMG_AnimationController animationController;

    //Game scene initialization.
    void Start()
    {
        //Get the main camera / animation controller.
        mainCamera = Camera.main;
        animationController = FindObjectOfType<GRHBalloonMG_AnimationController>();

        //Set the AI Controller [Added by Bryce]
        aiController = GetComponent<GRHBalloonMG_AIController>();

        //Object initialization is called here to prevent object racing. We check for objects that aren't necessarily required, such as the camera controller.
        if (mainCamera.GetComponent<GRHCameraController>())
        {
            mainCamera.GetComponent<GRHCameraController>().Initialize();
        }

        animationController.Initialize();

        //Set all players to active.
        for (int i = 0; i < 4; i++)
        {
            activePlayers[i] = true;
        }

        //Get the max balloon pumps from the difficulty selection script here. Using a temp value for now.
        maxBalloonPumps = 10;

        currentBalloonPumps = 0;

        //After initialization, set the game state to the introduction, and start the main camera movements, if we have a main camera controller.
        currentGameState = BalloonPopGameStates.Introduction;
        if (mainCamera.GetComponent<GRHCameraController>())
        {
            mainCamera.GetComponent<GRHCameraController>().BeginInitialMovements();
        }

        //Hide the player UI.
        HidePlayerUI();
    }

    //Game advancement method.
    protected override void AdvanceGame()
    {
        Debug.Log("Advancing game.");
        switch (currentGameState)
        {
            //The introduction state will run from the game fading in, until the camera has finished setting the scene and we're ready to go.
            case BalloonPopGameStates.Introduction:
                //Introduction has finished. Begin the first turn.
                StartCoroutine(BeginFirstTurn());
                break;

            //The player turn branch.
            case BalloonPopGameStates.PlayerTurn:
                //We're advancing the game from the player turn, so no matter what, we're hiding the player UI.
                HidePlayerUI();
                break;

            //AI #1 turn branch.
            case BalloonPopGameStates.AI1Turn:
                PumpBalloon(aiController.GeneratePumpAmount(maxBalloonPumps - currentBalloonPumps));
                break;

            //AI #2 turn branch.
            case BalloonPopGameStates.AI2Turn:
                PumpBalloon(aiController.GeneratePumpAmount(maxBalloonPumps - currentBalloonPumps));
                break;

            //AI #3 turn branch.
            case BalloonPopGameStates.AI3Turn:
                PumpBalloon(aiController.GeneratePumpAmount(maxBalloonPumps - currentBalloonPumps));
                break;

            //The game end state will show the end of game visuals (ie. You Lost! or You Won!), and will run until the hub is loaded afterwards.
            case BalloonPopGameStates.GameEnd:

                break;

            //Default branch where we should never end up.
            default:
                Debug.Log("Main game state not recognized.");
                break;
        }
    }

    //Ask if the game has finished or not. Returns true if it has, false if it hasn't.
    bool HasGameFinished()
    {
        bool gameFinished = false;

        //Game will determine if all the AIs are knocked out, or if the player is.
        //Start with checking AI status.
        if (activePlayers[1] == false && activePlayers[2] == false && activePlayers[3] == false)
        {
            //All AI are knocked out. Game has finished.
            gameFinished = true;
        }

        //If there's still an AI left, check if the player's still active.
        if (!gameFinished && activePlayers[0] == false)
        {
            gameFinished = true;
        }

        //Return the final value.
        return gameFinished;
    }

    //Determine who the next active player is, and return the associated state.
    BalloonPopGameStates DetermineNextActivePlayer(int currentPlayerPosition)
    {
        //Create the return value. Assigns a value to prevent compiler errors.
        BalloonPopGameStates nextActivePlayer = BalloonPopGameStates.PlayerTurn;

        //To handle looping around, we'll use a bool to determine if we need to loop around at all.
        bool hasDeterminedNextPlayer = false;

        //Create the int to hold the next player's position.
        int nextPlayerPosition = 0;

        //If this is the last player, there's no one ahead to look at.
        if (currentPlayerPosition != 3)
        {
            //Check if the players ahead of the current player are active.
            for (int i = currentPlayerPosition + 1; i < 4; i++)
            {
                if (activePlayers[i] == true)
                {
                    //This player is active. Set the player position, set the determination bool to true, and break out of the loop.
                    nextPlayerPosition = i;
                    hasDeterminedNextPlayer = true;
                    break;
                }
            }
        }

        //Next, handle the players behind the current player, if none ahead were active.
        if (!hasDeterminedNextPlayer)
        {
            for (int i = 0; i < currentPlayerPosition; i++)
            {
                if (activePlayers[i] == true)
                {
                    //This player is active. Set the player position, and break out of the loop.
                    nextPlayerPosition = i;
                    break;
                }
            }
        }

        //By this point, we know the next active player by the position we have.
        switch (nextPlayerPosition)
        {
            case 0:
                nextActivePlayer = BalloonPopGameStates.PlayerTurn;
                Debug.Log("Player turn.");
                break;

            case 1:
                nextActivePlayer = BalloonPopGameStates.AI1Turn;
                Debug.Log("AI 1 turn.");
                break;

            case 2:
                nextActivePlayer = BalloonPopGameStates.AI2Turn;
                Debug.Log("AI 2 turn.");
                break;

            case 3:
                nextActivePlayer = BalloonPopGameStates.AI3Turn;
                Debug.Log("AI 3 turn.");
                break;

            default:
                Debug.Log("DetermineNextActivePlayer returned a non-existent player.");
                break;
        }

        //Return the proper game state.
        return nextActivePlayer;
    }

    //Knock out the current player.
    internal void KnockOutCurrentPlayer()
    {
        //Set active to false depending on the current state.
        switch(currentGameState)
        {
            case BalloonPopGameStates.PlayerTurn:
                Debug.Log("Knocking out Player.");
                activePlayers[0] = false;
                break;

            case BalloonPopGameStates.AI1Turn:
                Debug.Log("Knocking out AI 1.");
                activePlayers[1] = false;
                break;

            case BalloonPopGameStates.AI2Turn:
                Debug.Log("Knocking out AI 2.");
                activePlayers[2] = false;
                break;

            case BalloonPopGameStates.AI3Turn:
                Debug.Log("Knocking out AI 3.");
                activePlayers[3] = false;
                break;

            default:
                Debug.Log("KnockOutCurrentPlayer attempting to knock out a non-existent player.");
                break;
        }
    }

    //Add a number of pumps to the balloon. Method is public to allow UI buttons to use it.
    public void PumpBalloon(int numberOfPumps)
    {
        //If this is the player pumping the balloon, hide the UI to keep them from mashing the button.
        if (currentGameState == BalloonPopGameStates.PlayerTurn)
        {
            HidePlayerUI();
        }

        //Add the pumps.
        currentBalloonPumps += numberOfPumps;

        Debug.Log(currentBalloonPumps);

        //Are we at or above the maximum number of pumps?
        if (currentBalloonPumps >= maxBalloonPumps)
        {
            //We are. Knock out the current player.
            KnockOutCurrentPlayer();
            currentBalloonPumps = 0;
        }

        //When done pumping, the turn ends. We'll call EndTurn, and that will handle the next steps.
        StartCoroutine(EndTurn());
    }

    //Method to display end of game visuals/animations.
    void EndGame()
    {
        Debug.Log("Game is ending.");
        currentGameState = BalloonPopGameStates.GameEnd;
        AdvanceGame();
    }

    //End of turn function. Determines whether the game has ended, the animation to play, and who the next player is.
    IEnumerator EndTurn()
    {
        //Set up the variables for animations. We default these to prevent compiler errors from the switch statements not liking us handling every case.
        GRHBalloonMG_AnimationController.AnimationObject target1 = GRHBalloonMG_AnimationController.AnimationObject.Player, target2 = GRHBalloonMG_AnimationController.AnimationObject.Player;
        GRHBalloonMG_AnimationController.AnimationLocation destination1 = GRHBalloonMG_AnimationController.AnimationLocation.Pump, destination2 = GRHBalloonMG_AnimationController.AnimationLocation.Pump;

        //First off, has the game ended?
        if (HasGameFinished())
        {
            //It has. That means the character currently at the pump needs to be taken away as a single movement. Determine the character to be moved.
            //We can find this by whose turn it currently is.
            switch(currentGameState)
            {
                case BalloonPopGameStates.PlayerTurn:
                    target1 = GRHBalloonMG_AnimationController.AnimationObject.Player;
                    break;

                case BalloonPopGameStates.AI1Turn:
                    target1 = GRHBalloonMG_AnimationController.AnimationObject.AI1;
                    break;

                case BalloonPopGameStates.AI2Turn:
                    target1 = GRHBalloonMG_AnimationController.AnimationObject.AI2;
                    break;

                case BalloonPopGameStates.AI3Turn:
                    target1 = GRHBalloonMG_AnimationController.AnimationObject.AI3;
                    break;

                default:
                    Debug.Log("Game manager's end turn is calling for a non-existent player.");
                    break;
            }

            //The destination will be the same regardless of who it is.
            destination1 = GRHBalloonMG_AnimationController.AnimationLocation.StartingLocation;

            //Now, start the animation, and wait for the right amount of time.
            animationController.MoveSingleCharacterToLocation(target1, destination1);
            yield return new WaitForSeconds(animationController.GetTimeForSingleMovement());

            //Now, we call EndGame.
            EndGame();
        } else
        {
            //Determine the first target and their destination.
            switch(currentGameState)
            {
                case BalloonPopGameStates.PlayerTurn:
                    //If we're in the player state, and the player was knocked out, it would be picked up in the HasGameEnded check earlier.
                    //Therefore, if it's the player's turn, we're returning them to their position.
                    target1 = GRHBalloonMG_AnimationController.AnimationObject.Player;
                    destination1 = GRHBalloonMG_AnimationController.AnimationLocation.PlayerLocation;

                    //Determine the next game state.
                    currentGameState = DetermineNextActivePlayer(0);
                    break;

                case BalloonPopGameStates.AI1Turn:
                    //First target is AI 1.
                    target1 = GRHBalloonMG_AnimationController.AnimationObject.AI1;

                    if (activePlayers[1])
                    {
                        //AI 1 is still active. Return them to their normal location.
                        destination1 = GRHBalloonMG_AnimationController.AnimationLocation.AI1Location;
                    } else
                    {
                        //AI 1 was knocked out. Put them off the screen.
                        destination1 = GRHBalloonMG_AnimationController.AnimationLocation.StartingLocation;
                    }

                    //Determine the next game state.
                    currentGameState = DetermineNextActivePlayer(1);
                    break;

                case BalloonPopGameStates.AI2Turn:
                    //First target is AI 2.
                    target1 = GRHBalloonMG_AnimationController.AnimationObject.AI2;

                    if (activePlayers[2])
                    {
                        //AI 2 is still active. Return them to their normal location.
                        destination1 = GRHBalloonMG_AnimationController.AnimationLocation.AI2Location;
                    }
                    else
                    {
                        //AI 2 was knocked out. Put them off the screen.
                        destination1 = GRHBalloonMG_AnimationController.AnimationLocation.StartingLocation;
                    }

                    //Determine the next game state.
                    currentGameState = DetermineNextActivePlayer(2);
                    break;

                case BalloonPopGameStates.AI3Turn:
                    //First target is AI 3.
                    target1 = GRHBalloonMG_AnimationController.AnimationObject.AI3;

                    if (activePlayers[3])
                    {
                        //AI 3 is still active. Return them to their normal location.
                        destination1 = GRHBalloonMG_AnimationController.AnimationLocation.AI3Location;
                    }
                    else
                    {
                        //AI 3 was knocked out. Put them off the screen.
                        destination1 = GRHBalloonMG_AnimationController.AnimationLocation.StartingLocation;
                    }

                    //Determine the next game state.
                    currentGameState = DetermineNextActivePlayer(3);
                    break;

                default:
                    Debug.Log("Game manager's end turn is checking a non-existent player.");
                    break;
            }

            //Now, using our new game state, determine the second target.
            switch(currentGameState)
            {
                case BalloonPopGameStates.PlayerTurn:
                    target2 = GRHBalloonMG_AnimationController.AnimationObject.Player;
                    break;

                case BalloonPopGameStates.AI1Turn:
                    target2 = GRHBalloonMG_AnimationController.AnimationObject.AI1;
                    break;

                case BalloonPopGameStates.AI2Turn:
                    target2 = GRHBalloonMG_AnimationController.AnimationObject.AI2;
                    break;

                case BalloonPopGameStates.AI3Turn:
                    target2 = GRHBalloonMG_AnimationController.AnimationObject.AI3;
                    break;

                default:
                    Debug.Log("Game manager's end turn is checking a non-existent player.");
                    break;
            }

            //The second destination is always the same.
            destination2 = GRHBalloonMG_AnimationController.AnimationLocation.Pump;

            //Now, start the animation, and wait the proper amount of time.
            animationController.MoveDoubleCharacterToLocations(target1, destination1, target2, destination2);
            yield return new WaitForSeconds(animationController.GetTimeForDoubleMovement());

            //Animation has played. If this is the player turn, show the UI. Otherwise, advance the game.
            if (currentGameState == BalloonPopGameStates.PlayerTurn)
            {
                ShowPlayerUI();
            } else
            {
                AdvanceGame();
            }
        }
    }

    //Handle initial turn.
    IEnumerator BeginFirstTurn()
    {
        //Move the player to the pump.
        animationController.MoveSingleCharacterToLocation(GRHBalloonMG_AnimationController.AnimationObject.Player, GRHBalloonMG_AnimationController.AnimationLocation.Pump);
        yield return new WaitForSeconds(animationController.GetTimeForSingleMovement());

        //Set the proper game state, and show the player UI.
        currentGameState = BalloonPopGameStates.PlayerTurn;
        ShowPlayerUI();
    }
}