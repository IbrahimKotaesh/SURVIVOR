mergeInto(LibraryManager.library, {
    SendGameOverToAndroid: function(score, coins) {
        if (typeof window.AndroidInterface !== 'undefined' && window.AndroidInterface.onGameOver) {
            window.AndroidInterface.onGameOver(score, coins);
        } else {
            console.log("AndroidInterface.onGameOver not found. Score: " + score + ", Coins: " + coins);
        }
    },
    SendVictoryToAndroid: function(score, coins, bonus) {
        if (typeof window.AndroidInterface !== 'undefined' && window.AndroidInterface.onVictory) {
            window.AndroidInterface.onVictory(score, coins, bonus);
        } else {
            console.log("AndroidInterface.onVictory not found. Score: " + score + ", Coins: " + coins + ", Bonus: " + bonus);
        }
    },
    SendGemsUpdatedToAndroid: function(newBalance) {
        if (typeof window.AndroidInterface !== 'undefined' && window.AndroidInterface.onGemsUpdated) {
            window.AndroidInterface.onGemsUpdated(newBalance);
        } else {
            console.log("AndroidInterface.onGemsUpdated not found. New Balance: " + newBalance);
        }
    }
});
