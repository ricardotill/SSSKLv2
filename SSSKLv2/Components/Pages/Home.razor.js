export class Home {
    static init() {
        const target = document.querySelector('.btn-select-checkbox');
        console.log("ye");
        target.addEventListener('change', e => {
            if (e.currentTarget.checked) {
                target.parentElement.classList.add("active")
            } else {
                target.parentElement.classList.remove("active")
            }
        });
        target.parentElement.addEventListener('click', e => {
            
        });
    }
    
    static getProducts() {
        
    }
}

window.Home = Home;